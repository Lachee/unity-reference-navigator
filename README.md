# Unity Reference Navigator

[![Build status](https://ci.appveyor.com/api/projects/status/b2b9r2eqyjgd9apu?svg=true)](https://ci.appveyor.com/project/Lachee/unity-reference-navigator)

Find all References of assets in Unity3D. It searches the GUID in every asset and lists all assets that contain it. Where possible it will display additional details about the asset such as components that reference the script.
Additionally, when searching for scripts, it will use the Roslyn compiler to analyise the code and determine code-based references to that Component.

## Install
A package can be found under the [AppVeyor artifacts](https://ci.appveyor.com/project/Lachee/unity-reference-navigator/build/artifacts).
