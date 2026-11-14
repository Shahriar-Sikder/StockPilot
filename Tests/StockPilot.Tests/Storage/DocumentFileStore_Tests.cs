using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using StockPilot.Services.Common;
using Xunit;

namespace StockPilot.Tests.Storage
{
    /// <summary>DocumentFileStore against a throwaway folder: where documents go, byte-for-byte content and the path guard.</summary>
    public class DocumentFileStore_Tests : IDisposable
    {
        private static readonly byte[] Pdf = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x01, 0x02, 0xFF };

        private readonly string _site;
        private readonly string _root;
        private readonly DocumentFileStore _store;

        public DocumentFileStore_Tests()
        {
            _site = Path.Combine(Path.GetTempPath(), "sp-documents-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_site);
            _root = Path.Combine(_site, "Pictures");
            _store = new DocumentFileStore("~/Pictures", _site);
        }

        public void Dispose()
        {
            if (Directory.Exists(_site)) Directory.Delete(_site, true);
        }

        [Fact]
        public void Documents_live_in_the_documents_folder_of_the_picture_root()
        {
            _store.DocumentsPath.ShouldBe(Path.Combine(_root, "documents"));
        }

        [Fact]
        public async Task Save_writes_the_bytes_unchanged_under_the_tenant_folder()
        {
            string path = await _store.SaveAsync(7, Pdf, ".PDF");

            path.ShouldMatch("^documents/t7/[0-9a-f]{32}\\.pdf$");
            string file = Path.Combine(_root, "documents", "t7", path.Split('/').Last());
            File.ReadAllBytes(file).ShouldBe(Pdf);
            Directory.GetFiles(Path.Combine(_root, "documents", "t7")).Length.ShouldBe(1); // no .tmp left behind
        }

        [Fact]
        public async Task A_document_without_a_tenant_goes_to_the_host_folder()
        {
            (await _store.SaveAsync(null, Pdf, ".pdf")).ShouldStartWith("documents/host/");
        }

        [Fact]
        public async Task Every_save_gets_a_new_name()
        {
            string first = await _store.SaveAsync(7, Pdf, ".pdf");
            string second = await _store.SaveAsync(7, Pdf, ".pdf");
            first.ShouldNotBe(second);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("pdf")]
        [InlineData(".")]
        [InlineData(".toolong")]
        [InlineData(".p/df")]
        [InlineData(".p df")]
        public async Task Save_refuses_an_unsafe_extension(string extension)
        {
            await Should.ThrowAsync<ArgumentException>(() => _store.SaveAsync(7, Pdf, extension));
            Directory.Exists(_store.DocumentsPath).ShouldBeFalse();
        }

        [Fact]
        public async Task Save_refuses_empty_content()
        {
            await Should.ThrowAsync<ArgumentException>(() => _store.SaveAsync(7, new byte[0], ".pdf"));
        }

        [Fact]
        public async Task Read_returns_the_saved_bytes_and_null_for_a_missing_file()
        {
            string path = await _store.SaveAsync(7, Pdf, ".pdf");

            (await _store.ReadOrNullAsync(path)).ShouldBe(Pdf);
            (await _store.ReadOrNullAsync("documents/t7/00000000000000000000000000000000.pdf")).ShouldBeNull();
        }

        [Fact]
        public async Task Delete_removes_the_file_and_ignores_empty_or_missing_paths()
        {
            string path = await _store.SaveAsync(7, Pdf, ".pdf");

            await _store.DeleteAsync(path);
            (await _store.ReadOrNullAsync(path)).ShouldBeNull();
            await _store.DeleteAsync(path);
            await _store.DeleteAsync(null);
            await _store.DeleteAsync("");
        }

        [Theory]
        [InlineData("documents/../web.config")]
        [InlineData("documents/t7/../../t7/items/a.png")]
        [InlineData("t7/items/a.png")]
        [InlineData("/Pictures/documents/t7/a.pdf")]
        [InlineData("documents/C:/Windows/win.ini")]
        [InlineData("documents//server/share/a.pdf")]
        [InlineData("   ")]
        public async Task Paths_outside_the_documents_folder_are_refused(string path)
        {
            Should.Throw<ArgumentException>(() => _store.MapToPhysicalPath(path));
            await Should.ThrowAsync<ArgumentException>(() => _store.ReadOrNullAsync(path));
        }

        [Fact]
        public async Task Delete_reports_a_refused_path_through_the_task()
        {
            Task delete = _store.DeleteAsync("documents/../../outside.txt"); // must not throw synchronously
            await Should.ThrowAsync<ArgumentException>(() => delete);
        }

        [Fact]
        public void Backslashes_are_accepted_in_a_stored_path()
        {
            _store.MapToPhysicalPath("documents\\t7\\a.pdf").ShouldBe(Path.Combine(_root, "documents", "t7", "a.pdf"));
        }

        [Theory]
        [InlineData(".pdf", true)]
        [InlineData(".jpeg", true)]
        [InlineData(".xlsx", true)]
        [InlineData("", false)]
        [InlineData(".pdf.exe", false)]
        public void IsSafeExtension(string extension, bool expected)
        {
            DocumentFileStore.IsSafeExtension(extension).ShouldBe(expected);
        }
    }
}
