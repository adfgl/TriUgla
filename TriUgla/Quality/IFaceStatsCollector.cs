namespace TriUgla;

public interface IFaceStatsCollector
{
    bool TryCollect(Face face, out FaceStats stats);
}
