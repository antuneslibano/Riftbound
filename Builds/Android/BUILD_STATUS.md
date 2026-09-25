# Android build status

**No APK has been generated yet.**

The environment where this project was written had no Unity Editor, Android Build Support, Android SDK or NDK.
Its network policy also blocked the Unity and Google download servers, so a Unity license could not be activated
either.

To produce `Builds/Android/RiftboundPrototype.apk`, open the project in Unity 6 (6000.0.x) with Android Build
Support + OpenJDK + Android SDK & NDK Tools installed, then either:

- use the menu **Riftbound > Build Android APK**, or
- run
  `Unity -batchmode -quit -projectPath <Riftbound> -buildTarget Android -executeMethod Riftbound.EditorTools.RiftboundBuild.BuildAndroidCommandLine -logFile build.log`

The APK is written to this folder.
