# RemoteDebug

Remote Debugging for VS2017 Open Folder feature

Setup:

* Ensure the remote machine has [Remote Debugging](https://docs.microsoft.com/en-us/visualstudio/debugger/remote-debugging) setup and `msvsmon` is running.
* Install this extension by running `RemoteDebug.vsix`.
* Open your project folder.
* Add a new `RemoteDebug.xml` settings file to `${workspaceRoot}\.vs\RemoteDebug.xml` that contains:
  ```xml
  <RemoteDebug>
    <MachineName>Machine name visible to local network</MachineName>
    <Path>Full path to executable on remote machine</Path>
  </RemoteDebug>
  ```
* Select `Tools|Remote Debug: Launch` to launch your debug session on the remote machine.
