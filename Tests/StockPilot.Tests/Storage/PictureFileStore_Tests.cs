using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Shouldly;
using StockPilot.Services.Common;
using Xunit;

namespace StockPilot.Tests.Storage
{
    /// <summary>PictureFileStore against a throwaway folder: paths, URLs, the ".." guard and replace order.</summary>
    public class PictureFileStore_Tests : IDisposable
    {
        private readonly string _site;
        private readonly string _root;
        private readonly PictureFileStore _store;

        public PictureFileStore_Tests()
        {
            _site = Path.Combine(Path.GetTempPath(), "sp-pictures-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_site);
            _root = Path.Combine(_site, "Pictures");
            _store = new PictureFileStore("~/Pictures", "/Pictures/", _site);
        }

        public void Dispose()
        {
            if (Directory.Exists(_site)) Directory.Delete(_site, true);
        }

        private static string Png(int width = 10, int height = 10)
        {
            using (var bitmap = new Bitmap(width, height))
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        /* ---------- dependency injection ---------- */

        [Fact]
        public void The_container_creates_the_store_from_the_app_settings()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<IPictureFileStore>().ImplementedBy<PictureFileStore>().LifestyleTransient());

                var store = (PictureFileStore)container.Resolve<IPictureFileStore>();

                // No settings in the test host, so the defaults: ~/Pictures under the application folder, /Pictures/.
                store.RootPath.ShouldBe(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), "Pictures"));
                store.BaseUrl.ShouldBe("/Pictures/");
            }
        }

        /* ---------- path building ---------- */

        [Fact]
        public void Root_is_the_virtual_path_inside_the_site_or_an_absolute_folder()
        {
            _store.RootPath.ShouldBe(_root);
            new PictureFileStore(null, null, _site).RootPath.ShouldBe(_root); // defaults: ~/Pictures and /Pictures/
            new PictureFileStore("Uploads\\Pics", null, _site).RootPath.ShouldBe(Path.Combine(_site, "Uploads", "Pics"));

            var outside = Path.Combine(Path.GetTempPath(), "sp-absolute-pictures");
            new PictureFileStore(outside + "\\", null, _site).RootPath.ShouldBe(outside);
        }

        [Fact]
        public void Base_url_is_normalized_and_must_be_relative()
        {
            new PictureFileStore(null, null, _site).BaseUrl.ShouldBe("/Pictures/");
            new PictureFileStore(null, "Media/Pics", _site).BaseUrl.ShouldBe("/Media/Pics/");
            new PictureFileStore(null, "~/Pictures", _site).BaseUrl.ShouldBe("/Pictures/");
            Should.Throw<ConfigurationErrorsException>(() => new PictureFileStore(null, "http://localhost:31892/Pictures/", _site));
        }

        [Fact]
        public async Task Save_writes_a_png_under_tenant_and_folder_and_returns_the_relative_url()
        {
            var url = await _store.SaveAsync(3, PictureFolders.Items, Png());

            url.ShouldStartWith("/Pictures/t3/items/");
            url.ShouldEndWith(".png");
            url.ShouldNotContain("://");

            var path = _store.MapToPhysicalPath(url);
            Path.GetDirectoryName(path).ShouldBe(Path.Combine(_root, "t3", "items"));
            Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out _).ShouldBeTrue(); // our name, not the user's
            File.Exists(path).ShouldBeTrue();
            using (var image = Image.FromFile(path)) image.RawFormat.Guid.ShouldBe(ImageFormat.Png.Guid);
            Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories).ShouldBeEmpty(); // no half-written file left
        }

        [Fact]
        public async Task Each_kind_of_picture_has_its_own_folder_and_unknown_folders_are_refused()
        {
            (await _store.SaveAsync(3, PictureFolders.Categories, Png())).ShouldStartWith("/Pictures/t3/categories/");
            (await _store.SaveAsync(3, PictureFolders.SubCategories, Png())).ShouldStartWith("/Pictures/t3/sub-categories/");

            await Should.ThrowAsync<ArgumentException>(() => _store.SaveAsync(3, "..\\..\\elsewhere", Png()));
            await Should.ThrowAsync<ArgumentException>(() => _store.SaveAsync(3, "logos", Png()));
            await Should.ThrowAsync<ArgumentException>(() => _store.SaveAsync(3, null, Png()));
        }

        [Fact]
        public async Task Two_saves_never_share_a_file_name()
        {
            var first = await _store.SaveAsync(3, PictureFolders.Items, Png());
            var second = await _store.SaveAsync(3, PictureFolders.Items, Png());
            second.ShouldNotBe(first);
        }

        /* ---------- tenant folder naming ---------- */

        [Fact]
        public async Task Tenant_folder_is_t_plus_id_and_host_without_a_tenant()
        {
            PictureFileStore.TenantFolder(3).ShouldBe("t3");
            PictureFileStore.TenantFolder(18).ShouldBe("t18");
            PictureFileStore.TenantFolder(null).ShouldBe("host");

            (await _store.SaveAsync(null, PictureFolders.Items, Png())).ShouldStartWith("/Pictures/host/items/");
            (await _store.SaveAsync(18, PictureFolders.Items, Png())).ShouldStartWith("/Pictures/t18/items/");
            Directory.Exists(Path.Combine(_root, "host", "items")).ShouldBeTrue();
            Directory.Exists(Path.Combine(_root, "t18", "items")).ShouldBeTrue();
        }

        /* ---------- URL <-> path ---------- */

        [Fact]
        public void Url_and_path_map_both_ways()
        {
            var path = Path.Combine(_root, "t3", "items", "abc.png");
            _store.ToUrl(path).ShouldBe("/Pictures/t3/items/abc.png");
            _store.MapToPhysicalPath("/Pictures/t3/items/abc.png").ShouldBe(path);
            _store.MapToPhysicalPath("/pictures/t3/items/abc.png?v=2").ShouldBe(path); // case and cache-busting query
        }

        [Theory]
        [InlineData("/Pictures/../Web.config")]
        [InlineData("/Pictures/t3/items/../../../Web.config")]
        [InlineData("/Pictures/t3/../../bin/StockPilot.Web.dll")]
        [InlineData("/Pictures/..\\..\\secret.png")]
        [InlineData("/Pictures/%2e%2e/secret.txt")]
        [InlineData("/Pictures//Windows/win.ini")]
        [InlineData("/Pictures/C:/Windows/win.ini")]
        [InlineData("/Other/t3/items/abc.png")]
        [InlineData("http://evil.example/Pictures/t3/items/abc.png")]
        [InlineData("/Pictures/t3/items/abc.exe")]
        [InlineData("/Pictures/")]
        [InlineData("")]
        public void Urls_outside_the_root_or_not_png_are_refused(string url)
        {
            Should.Throw<ArgumentException>(() => _store.MapToPhysicalPath(url));
        }

        [Fact]
        public void A_path_outside_the_root_has_no_url()
        {
            Should.Throw<ArgumentException>(() => _store.ToUrl(Path.Combine(_site, "Web.config")));
            // A sibling folder that only starts with the root's name is outside too.
            Should.Throw<ArgumentException>(() => _store.ToUrl(Path.Combine(_site, "PicturesBackup", "a.png")));
        }

        /* ---------- delete ---------- */

        [Fact]
        public async Task Delete_removes_the_file_and_ignores_empty_or_missing()
        {
            var url = await _store.SaveAsync(3, PictureFolders.Items, Png());
            var path = _store.MapToPhysicalPath(url);

            await _store.DeleteAsync(url);
            File.Exists(path).ShouldBeFalse();

            await _store.DeleteAsync(url);   // already gone
            await _store.DeleteAsync(null);
            await _store.DeleteAsync("  ");
        }

        [Fact]
        public async Task Delete_refuses_a_url_that_leaves_the_root_and_touches_nothing()
        {
            var secret = Path.Combine(_site, "secret.png");
            File.WriteAllText(secret, "keep me");

            await Should.ThrowAsync<ArgumentException>(() => _store.DeleteAsync("/Pictures/../secret.png"));
            await Should.ThrowAsync<ArgumentException>(() => _store.DeleteAsync("/Pictures/t3/items/../../../secret.png"));

            File.Exists(secret).ShouldBeTrue();
        }

        /* ---------- replace ---------- */

        [Fact]
        public async Task Replace_saves_the_new_picture_before_deleting_the_old_one()
        {
            var oldUrl = await _store.SaveAsync(3, PictureFolders.Items, Png());
            var oldPath = _store.MapToPhysicalPath(oldUrl);

            var newUrl = await _store.ReplaceAsync(3, PictureFolders.Items, oldUrl, Png(20, 20));

            newUrl.ShouldNotBe(oldUrl);
            File.Exists(_store.MapToPhysicalPath(newUrl)).ShouldBeTrue();
            File.Exists(oldPath).ShouldBeFalse();
            Directory.GetFiles(Path.Combine(_root, "t3", "items")).Length.ShouldBe(1);
        }

        [Fact]
        public async Task Replace_keeps_the_old_picture_when_the_new_one_is_refused()
        {
            var oldUrl = await _store.SaveAsync(3, PictureFolders.Items, Png());
            var oldPath = _store.MapToPhysicalPath(oldUrl);

            await Should.ThrowAsync<UserFriendlyException>(() => _store.ReplaceAsync(3, PictureFolders.Items, oldUrl, "not a picture"));

            File.Exists(oldPath).ShouldBeTrue();
            Directory.GetFiles(Path.Combine(_root, "t3", "items")).Single().ShouldBe(oldPath);
        }

        [Fact]
        public async Task Replace_without_a_previous_picture_just_saves()
        {
            var url = await _store.ReplaceAsync(3, PictureFolders.Categories, null, Png());
            File.Exists(_store.MapToPhysicalPath(url)).ShouldBeTrue();
        }

        /* ---------- the same checks as PictureStore ---------- */

        [Fact]
        public async Task Invalid_empty_and_oversized_uploads_are_refused_and_write_nothing()
        {
            await Should.ThrowAsync<UserFriendlyException>(() => _store.SaveAsync(3, PictureFolders.Items, null));
            await Should.ThrowAsync<UserFriendlyException>(() => _store.SaveAsync(3, PictureFolders.Items, "%%% not base64 %%%"));
            await Should.ThrowAsync<UserFriendlyException>(() => _store.SaveAsync(3, PictureFolders.Items, Convert.ToBase64String(new byte[] { 1, 2, 3 })));

            var tooLarge = Convert.ToBase64String(new byte[PictureHelper.MaxUploadBytes + 1]);
            await Should.ThrowAsync<UserFriendlyException>(() => _store.SaveAsync(3, PictureFolders.Items, tooLarge));

            if (Directory.Exists(_root)) Directory.GetFiles(_root, "*", SearchOption.AllDirectories).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_data_url_is_accepted_and_a_large_picture_is_resized()
        {
            var url = await _store.SaveAsync(3, PictureFolders.Items, "data:image/png;base64," + Png(880, 440));

            using (var image = Image.FromFile(_store.MapToPhysicalPath(url)))
            {
                image.Width.ShouldBe(PictureHelper.MaxDimension);
                image.Height.ShouldBe(PictureHelper.MaxDimension / 2);
            }
        }
    }
}
