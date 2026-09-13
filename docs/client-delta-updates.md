# Client Delta Updates

The launcher can update installed game clients from static files hosted under the server update URL.
The update URL can be a normal web folder, a GitHub repository URL, or a GitHub tree URL.

Expected layout:

```text
https://your-update-host/
  client/
    manifest.json
    files/
      bin32_archeage.exe
      game_config_example.dat
```

`manifest.json` uses the same simple shape as launcher releases:

```json
{
  "version": "2026.09.13.1",
  "files": [
    {
      "path": "bin32/archeage.exe",
      "sha256": "file hash",
      "size": 123456
    }
  ]
}
```

Generate the package from a prepared client folder:

```powershell
.\scripts\Publish-ClientDelta.ps1 -ClientRoot "C:\AA\Client" -OutputRoot ".\publish" -Version "2026.09.13.1"
```

Upload the generated `publish/client` folder to the root of the configured `serverGameUpdateURL`.
The launcher downloads only files whose local SHA-256 does not match the manifest, then records the installed manifest in `.aaemu-client-manifest.json`.

GitHub examples:

```text
https://github.com/Crybb227/AA-Emu-Client-AA
https://github.com/Crybb227/AA-Emu-Client-WoW
https://github.com/owner/repo
https://github.com/owner/repo/tree/main/publish
https://raw.githubusercontent.com/owner/repo/main/publish
```

For the second example, the launcher reads:

```text
https://raw.githubusercontent.com/owner/repo/main/publish/client/manifest.json
```
