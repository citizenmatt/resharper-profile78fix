using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.model2.Assemblies.Interfaces;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Profile78
{
    [SolutionComponent]
    public class Profile78SystemRuntimeProvider : IProjectPsiModuleProviderFilter
    {
        private readonly ChangeManager changeManager;
        private readonly IShellLocks shellLocks;
        private readonly IAssemblyFactory assemblyFactory;

        public Profile78SystemRuntimeProvider(ChangeManager changeManager, IShellLocks shellLocks, IAssemblyFactory assemblyFactory)
        {
            this.changeManager = changeManager;
            this.shellLocks = shellLocks;
            this.assemblyFactory = assemblyFactory;
        }

        public JetTuple<IProjectPsiModuleHandler, IPsiModuleDecorator> OverrideHandler(Lifetime lifetime,
            IProject project, IProjectPsiModuleHandler handler)
        {
            if (project.ProjectProperties.ProjectKind == ProjectKind.REGULAR_PROJECT)
            {
                var psiModules = project.GetComponent<IPsiModules>();
                var newHandler = new Handler(lifetime, handler, changeManager, assemblyFactory, project, shellLocks, psiModules);
                var decorator = new Decorator(newHandler);
                return new JetTuple<IProjectPsiModuleHandler, IPsiModuleDecorator>(newHandler, decorator);
            }

            return null;
        }

        private class Handler : DelegatingProjectPsiModuleHandler, IChangeProvider
        {
            private readonly ChangeManager changeManager;
            private readonly IAssemblyFactory assemblyFactory;
            private readonly IProject project;
            private readonly IPsiModules psiModules;
            private IAssemblyCookie assemblyCookie;

            public Handler(Lifetime lifetime, IProjectPsiModuleHandler handler, ChangeManager changeManager,
                           IAssemblyFactory assemblyFactory, IProject project, IShellLocks shellLocks, IPsiModules psiModules)
                : base(handler)
            {
                this.changeManager = changeManager;
                this.assemblyFactory = assemblyFactory;
                this.project = project;
                this.psiModules = psiModules;

                lifetime.AddAction(() => shellLocks.ExecuteWithWriteLock(() =>
                {
                    if (assemblyCookie != null)
                        assemblyCookie.Dispose();
                    assemblyCookie = null;
                }));

                changeManager.RegisterChangeProvider(lifetime, this);
                if (handler.ChangeProvider != null)
                    changeManager.AddDependency(lifetime, this, handler.ChangeProvider);

                changeManager.Changed2.Advise(lifetime, args =>
                {
                    var map = args.ChangeMap;

                    var psiModuleChange = map.GetChange<PsiModuleChange>(psiModules);
                    if (psiModuleChange != null)
                        OnPsiModuleChange(psiModuleChange);
                });
            }

            public override IChangeProvider ChangeProvider
            {
                get { return this; }
            }

            public object Execute(IChangeMap changeMap)
            {
                return BaseHandler.ChangeProvider != null ? changeMap.GetChange<PsiModuleChange>(BaseHandler.ChangeProvider) : null;
            }

            private void OnPsiModuleChange(PsiModuleChange change)
            {
                if (!change.ModuleChanges.Any(c => c.Item is IProjectPsiModule && Equals(((IProjectPsiModule)c.Item).Project, project)))
                    return;

                if (assemblyCookie != null || IsSystemRuntimeReferenced())
                    return;

                var systemRuntimeLocation = FindProfile78SystemRuntime();
                if (!systemRuntimeLocation.IsEmpty && assemblyCookie == null)
                    AddSystemRuntimeModule(systemRuntimeLocation);
            }

            private bool IsSystemRuntimeReferenced()
            {
                return (from module in psiModules.GetPsiModules(project)
                        from reference in psiModules.GetModuleReferences(module, module.GetContextFromModule())//*/ DiagnosticResolveContext.Instance);
                        where reference.Module.Name.Equals("System.Runtime", StringComparison.InvariantCultureIgnoreCase)
                        select reference).Any();
            }

            private FileSystemPath FindProfile78SystemRuntime()
            {
                var modules = psiModules.GetPsiModules(project);
                foreach (var module in modules)
                {
                    var directReferences = psiModules.GetModuleReferences(module, module.GetContextFromModule());//*/ DiagnosticResolveContext.Instance);
                    foreach (var directlyReferencedModule in directReferences.Select(r => r.Module).OfType<IAssemblyPsiModule>())
                    {
                        var secondLevelReferences = psiModules.GetModuleReferences(directlyReferencedModule,
                            directlyReferencedModule.GetContextFromModule());
                            //DiagnosticResolveContext.Instance);
                        foreach (var secondLevelModule in secondLevelReferences.Select(r => r.Module).OfType<IAssemblyPsiModule>())
                        {
                            if (secondLevelModule.Name.Equals("System.Runtime",
                                    StringComparison.InvariantCultureIgnoreCase) && IsProfile78(secondLevelModule))
                            {
                                try
                                {
                                    var platformId = project.PlatformID;
                                    var platformManager = project.GetComponent<PlatformManager>();
                                    platformId = platformManager.GetRunTimePlatformId(platformId);
                                    var platformInfo = platformManager.GetPlatformInfo(platformId);
                                    if (platformInfo != null)
                                    {
                                        var platformFoldersResult = platformInfo.PlatformFolders
                                            .Select(p => p.Combine(secondLevelModule.Name).AddSuffix(".dll"))
                                            .FirstOrDefault(p => p.ExistsFile);
                                        if (platformFoldersResult != null)
                                            return platformFoldersResult;
                                    }
                                }
                                catch
                                {
                                    // Do nothing
                                }
                            }
                        }
                    }
                }

                return FileSystemPath.Empty;
            }

            private bool IsProfile78(IAssemblyPsiModule asm)
            {
                var location = asm.Assembly.Location;
                return location != null && location.GetPathComponents().Contains("Profile78");
            }

            private void AddSystemRuntimeModule(FileSystemPath systemRuntimeLocation)
            {
                assemblyCookie = assemblyFactory.AddRef(systemRuntimeLocation, GetType().Name + "::" + project.Name,
                    project.GetResolveContext());

                var changeBuilder = new PsiModuleChangeBuilder();
                changeBuilder.AddModuleChange(PrimaryModule, PsiModuleChange.ChangeType.Added);
                changeManager.OnProviderChanged(ChangeProvider, changeBuilder.Result, SimpleTaskExecutor.Instance);
            }

            public IEnumerable<IPsiModule> GetModulesToReference()
            {
                if (assemblyCookie != null)
                {
                    var module = psiModules.GetPrimaryPsiModule(assemblyCookie.Assembly);
                    if (module != null)
                        yield return module;
                }
            }

            public bool HasModulesToReference { get { return GetModulesToReference().Any(); } }
        }

        private class Decorator : IPsiModuleDecorator
        {
            private readonly Handler handler;

            public Decorator(Handler handler)
            {
                this.handler = handler;
            }

            public IEnumerable<IPsiModuleReference> OverrideModuleReferences(IEnumerable<IPsiModuleReference> references)
            {
                if (!handler.HasModulesToReference)
                    return references;
                return references.Concat(handler.GetModulesToReference().Select(m => new PsiModuleReference(m)));
            }

            public IEnumerable<IPsiSourceFile> OverrideSourceFiles(IEnumerable<IPsiSourceFile> files)
            {
                return files;
            }
        }
    }
}