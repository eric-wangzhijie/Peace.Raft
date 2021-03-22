using System.Threading.Tasks;

namespace Raft.Demo
{
    public interface IHost
    {
       // ContainerResponse Invoke(ContainerRequest request);

        ClientResponse ClientInvoke(ClientReqeust request);

        AppendEntriesResponse AppendEntriesInvoke(AppendEntriesRequest reqeust);

        VoteResponse VoteInvoke(VoteReqeust voteReqeust);

        InstallSnapshotResponse InstalledSnapshotInvoke(InstallSnapshotReqeust installSnapshotReqeust);
    }
}