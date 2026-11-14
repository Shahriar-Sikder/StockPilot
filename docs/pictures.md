# Pictures and invoice documents as files

Pictures of **items**, **categories** and **sub-categories** are stored as PNG files on disk; the database keeps only
each picture's relative URL (`PictureUrl`). **Purchase invoice documents** (attachments) are files in the same folder,
under `documents\`; the database keeps only their path (`FilePath`). The tenant logo, custom CSS and user profile
pictures are still stored in the database (`AppBinaryObjects`).

## Where pictures live

| | |
|---|---|
| Folder | `StockPilot:PictureRootPath` in `Web.config` (default `~/Pictures`, i.e. `<site>\Pictures`) |
| URL prefix | `StockPilot:PictureBaseUrl` (default `/Pictures/`) |
| One picture | `<root>\t<tenantId>\<items \| categories \| sub-categories>\<guid>.png` (`host\` instead of `t<id>\` for the host) |
| In the database | `ItemMasters.PictureUrl`, `ItemCategories.PictureUrl`, `ItemSubCategories.PictureUrl`, e.g. `/Pictures/t3/items/9f1c….png` |

- Uploads are checked (max 3 MB, must be a picture) and resized to at most 220 px PNG; the file name is a new Guid,
  never the uploaded name.
- Replacing a picture writes the new file, saves the record, and only then deletes the old file. Removing a picture
  clears `PictureUrl` and then deletes the file. If the save fails, the new file is deleted again.
- Deleting a record (soft delete) keeps its picture, so Restore shows it again.
- The URL saved is relative: changing the domain or port does not break pictures. If you change `PictureBaseUrl`,
  existing rows keep the old prefix — only change it on a new installation.
- Code: `StockPilot.Application\Services\Common\PictureFileStore.cs`, `EntityPictureFiles.cs`, `PictureFileMigrator.cs`.

## Purchase invoice documents

| | |
|---|---|
| One document | `<root>\documents\t<tenantId>\<guid>.<ext>` — the extension of the uploaded name (`.pdf`, `.jpg`, …) |
| In the database | `PurchaseInvoiceDocuments.FilePath`, e.g. `documents/t3/9f1c….pdf`, plus the original `FileName`, `ContentType`, `SizeBytes` |

- **Private.** `Web.config` hides the `documents` folder (`hiddenSegments`), so `/Pictures/documents/...` is always
  404. Documents are only downloaded through the invoice screen (`PurchaseInvoice.DownloadDocument`), which checks the
  user's permission and tenant.
- Uploads are checked as before (allowed types, max 3 MB, content must match the extension) and stored byte for byte;
  the file name is a new Guid, never the uploaded name. If the save fails, the file is deleted again.
- Removing a document is a soft delete and keeps the file, so Restore brings it back.
- Code: `DocumentFileStore.cs` (same folder as above), `PurchaseInvoiceAppService.cs`.

## How they are served

`/Pictures/...` is served by IIS as plain static files, set up by `<location path="Pictures">` in `Web.config`:

- only `.png`, only GET/HEAD; scripts, config files and folder listings are refused (404);
- `X-Content-Type-Options: nosniff`;
- browsers keep a picture for a year (`Cache-Control: public, max-age=31536000`, set in `Global.asax.cs`), which is
  safe because a new picture always gets a new file name.

**Pictures are public by URL** (documents are not, see above). Anyone who has a picture's link can open it without signing in. Links are hard to
guess (random 128-bit names, no folder listing), but a copied or shared link keeps working. If store pictures must
ever require sign-in, serve them through a controller that checks the tenant instead (not built).

If you change `PictureBaseUrl`, change `<location path="Pictures">` in `Web.config` and the path check in
`Global.asax.cs` to match.

## Permissions

The IIS application pool account needs **Modify** on the pictures folder, documents included (to create folders,
write, rename and delete files):

```
icacls "D:\inetpub\StockPilot\Pictures" /grant "IIS APPPOOL\StockPilot:(OI)(CI)M"
```

Use the folder from `PictureRootPath` and your app pool's name. Without this, uploads fail with an access error and
the log (`App_Data\Logs\Logs.txt`) shows the path.

## Backups

**Back up the pictures folder as well as the database.** A database backup alone no longer contains these pictures
or the invoice documents, and a folder backup alone has no records pointing to them. Back both up at the same time.

## Moving the folder outside the site (recommended for production)

Keeping pictures outside the site folder keeps them safe from publishing and makes backups simpler.

1. Create the folder, e.g. `D:\StockPilotData\Pictures`, and grant the app pool **Modify** (see above).
2. Stop the site, move the existing `t…`, `host` and `documents` folders from `<site>\Pictures` into it.
3. Set `<add key="StockPilot:PictureRootPath" value="D:\StockPilotData\Pictures" />` in `Web.config`.
4. In IIS Manager: right-click the site → **Add Virtual Directory** → Alias `Pictures`, Physical path
   `D:\StockPilotData\Pictures`. The `<location path="Pictures">` rules in `Web.config` apply to it as well.
5. Start the site and open a picture URL to check.

URLs in the database stay the same (`/Pictures/...`), so no data changes.

## Publishing

- The `Pictures` folder is never published (`ExcludeFoldersFromDeployment`), and a Web Deploy publish never deletes it
  on the server (`MsDeploySkipRules`) — both in `StockPilot.Web.csproj`.
- A **File System** publish with **"Delete all existing files prior to publish"** switched on deletes everything in the
  target that is not part of the build — uploaded pictures included. The project stops such a publish *before
  anything is deleted* when the target already has pictures (`StockPilotProtectUploadedPictures` in the csproj). Keep
  that option **off** in `Properties\PublishProfiles\StockPilot.pubxml` (it is off now), or keep pictures outside the
  site.
- Never publish into the source folder (`D:\Development\StockPilot`); the safe profile targets
  `D:\Shiblee\Development\Release\StockPilot`.

## Moving existing pictures and documents out of the database (one time)

Pictures saved before this change are in `AppBinaryObjects` (`PictureId` on the three tables), and so are invoice
documents (`PurchaseInvoiceDocuments.BinaryObjectId`). `PictureFileMigrator` moves both to files at site start, for
every tenant and including deleted rows. It is safe to run more than once. Until a document is moved it still
downloads from the database.

**Release A — the new code and the move:**

1. Deploy, with `<add key="StockPilot:PicturesMigratedToFiles" value="false" />` in `Web.config`.
2. Start the site. The log shows two lines, for example
   `Picture move to files: moved 120, skipped 0, missing 1, failed 0`
   `Document move to files: moved 35, skipped 0, missing 0, failed 0`
   - *missing*: the row pointed to a picture or document that no longer existed; its `PictureId` /
     `BinaryObjectId` was cleared;
   - *failed*: the row keeps its old picture (the error is logged above that line); fix it and restart to retry.
3. When both lines show `failed 0`, set `PicturesMigratedToFiles` back to `true`.

**Release B — removing the old column** (a later release): it drops `PictureId` from the three tables. Before
deploying it, check that nothing is left; all must be 0:

```sql
SELECT COUNT(*) FROM ItemMasters       WHERE PictureId IS NOT NULL;
SELECT COUNT(*) FROM ItemCategories    WHERE PictureId IS NOT NULL;
SELECT COUNT(*) FROM ItemSubCategories WHERE PictureId IS NOT NULL;
SELECT COUNT(*) FROM PurchaseInvoiceDocuments WHERE BinaryObjectId IS NOT NULL;
```

The same release can drop `PurchaseInvoiceDocuments.BinaryObjectId` and the database fallback in `DownloadDocument`.

The drop must not ship in the same release as the move: the site applies migrations at startup, before the move runs.

## Troubleshooting

| Symptom | Check |
|---|---|
| Initials instead of a picture | Open the item's `PictureUrl` in the browser. 404: the file is missing (moved folder, restore without the Pictures backup). |
| Upload fails with an access error | App pool **Modify** permission on the folder. |
| "Attachment file missing" on download | The file under `<root>\documents\` is gone (moved folder, restore without the Pictures backup). |
| Old pictures missing after deploying | `PicturesMigratedToFiles` was `true` before the move ran: set it to `false` and restart. |
| Pictures gone after a publish | "Delete all existing files" was on and the guard was bypassed: restore the Pictures folder from backup. |
