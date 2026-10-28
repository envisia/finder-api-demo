# envisia finder API demo

A small C# console app that shows how to use the [envisia finder REST API](https://static.envisia.de/envisia_finder/envisia-finder-api-doc-public-de.html): search folders, download documents and upload new ones. It is meant to be read: each feature is one class that goes through the API calls step by step, and the console prints every call it makes.

It does not use the envisia NuGet package, so the whole upload session protocol is in this repository as source code.

## Setup

You need the .NET 10 SDK and an app registration in the envisia finder: *Systemeinstellungen › App-Registrierungen › Hinzufügen*, type *envisia finder API*. Put the values from the downloaded configuration file into `appsettings.Local.json`, either in `src/FinderDemo` or in the directory you run the app from. Git ignores that file.

```json
{
  "Finder": {
    "ServerAddress": "https://customer.envisia-finder.de",
    "ClientId": "…",
    "ClientSecret": "…"
  }
}
```

You can also pass the values as environment variables (`FINDERDEMO_Finder__ClientSecret`) or on the command line (`--Finder:ClientId=…`). If something is missing, the app asks for it at startup. Two settings are optional:

- `Finder:ImpersonateUpn`: makes every call run as that user. It uses token exchange and needs the scope `finder.impersonation`.
- `DownloadDirectory`: where downloads are saved. The default is `~/Downloads/finder-demo`.

Downloads need the scope `finder.api.media`, which envisia support enables per app. Everything else works without it.

```bash
dotnet run --project src/FinderDemo
```

## Where to look

The solution `FinderDemo.slnx` has one project, `src/FinderDemo`. `Directory.Build.props` holds the shared build and lint settings, and `Directory.Packages.props` holds the package versions. `Program.cs` sets up the generic host (configuration, logging, DI) and starts the main menu.

| Folder in `src/FinderDemo` | Contents |
| --- | --- |
| `Areas/` | What the user sees. `Search/SearchArea.cs`, `Search/FolderArea.cs` and `Upload/UploadArea.cs` read top-down, one step per API call. |
| `Services/` | How the API is called. `FinderApiClient` covers search, locations, additionals, folders and download. `FinderUploadClient` covers the upload session. `FinderTokenProvider` gets the OAuth tokens, and `FinderAuthorizationHandler` adds them to every request. |
| `Models/` | Request and response bodies, grouped by API area. |
| `Extensions/` | Registration of the configuration sources, Serilog and the services, and small HTTP helpers. |
| `Logging/` | Source-generated `[LoggerMessage]` methods, and the Serilog sink that prints API calls to the console. |

## Search

`POST /api/v1/Search/Document` by folder name (`name`), EfinGuid (`id`), full text (`query`), or folders where additionals are **not set** (`empty_additionals`). The app prints the request body before it sends it. From the results you can open a folder (`GET /api/v2/Document/{efinGuid}`). There you can download a document (`GET /api/v2/media/{file}`, with the document's ID, not the folder's) or show its page text.

**Searching for additionals that are not set works.** `empty_additionals` takes the IDs of additionals a folder must not have. The IDs come from `POST /api/v1/Category/Additionals`.

```json
{ "empty_additionals": [12], "from_hit": 0, "hits_size": 10 }
```

It accepts numeric IDs only, not identifiers. `exclude_additionals` is a different filter: it excludes folders that have a specific value. The public spec does not say whether several IDs are combined with AND or OR.

## Upload

`UploadArea` goes through these steps in order:

1. **Choose the target:** one process step (`POST /api/v1/Category/Process`) or one or more archive categories (`POST /api/v1/Category/Archive`).
2. **Fill in the additionals:** they are loaded for that target with `POST /api/v1/Category/Additionals`. Each type gets a matching prompt: date, number, text with suggestions, select list or yes/no.
3. **Validate:** `POST /api/v2/Additional/Validate`. The app asks again for every additional the server rejects.
4. **Upload** with `FinderUploadClient`:
   - `POST /api/v2/CreateUploadSession` with the target, additionals and a checksum. `md5Hash` is the default; `crc32cHash` can be chosen instead.
   - One `PUT /api/v2/FileUploadSession/{key}` per fragment. Fragments are 8 MiB; every fragment except the last is a multiple of 256 KiB, and the last one is always smaller than the others.
   - Failed fragments are retried from the range the server reports. A failed upload deletes its session.
5. **Wait for processing:** poll `POST /api/v1/Search/Uploaded` until the new folder appears.

## Development

- **Lint:** `dotnet build` is the lint check. `envisia.ruleset` and `stylecop.json` are copied from `Envisia.Utils.Linting`, the public `StyleCop.Analyzers` package applies them, and warnings are errors.
- **Logs:** everything logs through `ILogger<T>` with Serilog as the provider. Serilog writes daily files to `logs/` (settings `LogDirectory` and `LogLevel`) and prints each API call to the console. The logs never contain tokens or the client secret, because a call is logged only with its method, path, status and duration.

## License

MIT, see [LICENSE](LICENSE).
