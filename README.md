# Unity Reference Navigator

[![Build status](https://ci.appveyor.com/api/projects/status/b2b9r2eqyjgd9apu?svg=true)](https://ci.appveyor.com/project/Lachee/unity-reference-navigator)

Find all References of assets in Unity3D. It searches the GUID in every asset and lists all assets that contain it. Where possible it will display additional details about the asset such as components that reference the script.
Additionally, when searching for scripts, it will use the Roslyn compiler to analyise the code and determine code-based references to that Component.

![Window Example](https://i.lu.je/2020/chrome_hKM8wp91ea.png)

## Install
A package can be found under the [AppVeyor artifacts](https://ci.appveyor.com/project/Lachee/unity-reference-navigator/build/artifacts).

## TODO
- We need support for Addressables, as they use completely different metadata.  
  - It will likely be a sub module that you add on as I dont wish to create a dependency for the addressables.
- Add support for ScriptableObjects
  - They work. However, they behave like MonoBehaviours at the moment. This causes their scripting reference to be searched rather than the asset itself. Should be a simple change "in theory".
