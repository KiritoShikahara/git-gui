using GitGui.Core.Models;

namespace GitGui.Core.Services;

/// <summary>
/// Assigns a lane (column) index to each commit in a topologically-ordered (newest first) commit list,
/// so the History view can render a simplified branch/merge graph similar to `git log --graph`.
/// </summary>
public static class CommitGraphBuilder
{
    public static void AssignLanes(IReadOnlyList<CommitModel> commitsNewestFirst)
    {
        // Maps a commit sha that is "expected next" in a lane to that lane's index.
        var laneOfAwaitedSha = new List<string?>();

        foreach (var commit in commitsNewestFirst)
        {
            int lane = laneOfAwaitedSha.FindIndex(sha => sha == commit.Sha);
            commit.HasIncomingEdge = lane != -1;

            if (lane == -1)
            {
                lane = laneOfAwaitedSha.FindIndex(sha => sha is null);
                if (lane == -1)
                {
                    lane = laneOfAwaitedSha.Count;
                    laneOfAwaitedSha.Add(null);
                }
            }

            commit.Lane = lane;

            var passThrough = new List<int>();
            for (int i = 0; i < laneOfAwaitedSha.Count; i++)
            {
                if (i != lane && laneOfAwaitedSha[i] is not null)
                {
                    passThrough.Add(i);
                }
            }
            commit.PassThroughLanes = passThrough;

            var parentLanes = new List<int>(commit.ParentShas.Count);

            if (commit.ParentShas.Count == 0)
            {
                laneOfAwaitedSha[lane] = null;
            }
            else
            {
                // First parent continues in the same lane.
                laneOfAwaitedSha[lane] = commit.ParentShas[0];
                parentLanes.Add(lane);

                // Additional parents (merges) get their own lane, reusing a free one if possible.
                for (int i = 1; i < commit.ParentShas.Count; i++)
                {
                    var parentSha = commit.ParentShas[i];

                    int existingLane = laneOfAwaitedSha.FindIndex(sha => sha == parentSha);
                    if (existingLane != -1)
                    {
                        parentLanes.Add(existingLane);
                        continue;
                    }

                    int freeLane = laneOfAwaitedSha.FindIndex(sha => sha is null);
                    if (freeLane == -1)
                    {
                        freeLane = laneOfAwaitedSha.Count;
                        laneOfAwaitedSha.Add(null);
                    }

                    laneOfAwaitedSha[freeLane] = parentSha;
                    parentLanes.Add(freeLane);
                }
            }

            commit.ParentLanes = parentLanes;
        }
    }
}
