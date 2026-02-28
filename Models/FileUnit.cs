using P2PShare.Libs.Models.FileSytem;

namespace P2PShare.Models
{
    public class FileUnit
    {
        public required string Path { get; init; }
        public required bool My { get; init; }
        public required Unit Unit { get; init; }
        public required int? ID { get; init; }
        public long Size { get; set; }
    }
}
