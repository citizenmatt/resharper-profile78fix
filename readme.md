# resharper-profile78fix

This plugin is a tactical fix to an issue with ReSharper 8.1 that occurs when you have a .net 4.5 project referencing a PCL that matches Profile 78. The issue is broadly described here - [RSRP-394695](http://youtrack.jetbrains.com/issue/RSRP-394695).

This plugin is not available on ReSharper's Extension Gallery, but hosted privately on a MyGet feed. You can add a new extension source in the Options dialog, or by clicking the "Settings" button on ReSharper's Extension Manager dialog. Add the following feed url:

    https://www.myget.org/F/rs/

This plugin should be considered unsupported and alpha quality. Use at own risk.

## Description of the issue

The ReactiveUI project is a good example which causes the issue to surface (to be clear, the fault is NOT in ReactiveUI). The .NET 4.5 projects reference the portable version of System.Reactive.Linq 2.2, from the `Portable-Net45+WinRT45+WP8` folder.

This version of System.Reactive.Linq is a PCL assembly that targets Profile 78. In turn, it references System.Runtime, also from Profile 78.

When building, msbuild unifies the versions, and the .net 4.5 version of System.Runtime is used during compilation. ReSharper does not unify the versions, and complains that the Profile 78 version of System.Runtime is required. 

This plugin is a blunt instrument that tells ReSharper to add the .net 4.5 version of System.Runtime to its caches, so it no longer complains. It is hiding the effects of the issue, not fixing it. There should be no nasty side effects to this, but caveat emptor - no warranties or guarantees are given.

The plugin is pinned to the 8.1 RTM release of ReSharper, and isn't in the official extension gallery. The bug should hopefully be fixed soon, so this is a workaround until then.    