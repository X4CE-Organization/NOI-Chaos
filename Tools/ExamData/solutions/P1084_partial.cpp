#include <iostream>
#include<cstdio>
#include<cstring>
#include<queue>
#include<cmath>
#include<algorithm>
#define ll long long
using namespace std;
const int N=6e4;
int n,m,t,tot=0,atot=0,btot=0,ctot=0;
int d[N],query[N],f[N][20];
int ver[2*N],edge[2*N],Next[2*N],head[N];
bool ok,sta[N],need[N];
ll ans,tim[N],ned[N],dist[N][20];
pair<ll,int> h[N];
queue<int> q;
void add(int x,int y,int z){ver[++tot]=y;edge[tot]=z;Next[tot]=head[x];head[x]=tot;}
void bfs()
{
    q.push(1);d[1]=1;
    while(q.size())
    {
        int x=q.front();q.pop();
        for(int i=head[x];i;i=Next[i])
        {
            int y=ver[i]; if(d[y]) continue; d[y]=d[x]+1;
            f[y][0]=x;dist[y][0]=edge[i];
            for(int j=1;j<=t;j++){f[y][j]=f[f[y][j-1]][j-1];dist[y][j]=dist[y][j-1]+dist[f[y][j-1]][j-1];}
            q.push(y);
        }
    }
}
bool dfs(int x)
{
    bool pson=0; if(sta[x]) return 1;
    for(int i=head[x];i;i=Next[i])
    {
        int y=ver[i]; if(d[y]<d[x]) continue; pson=1; if(!dfs(y)) return 0;
    }
    return pson;
}
bool check(ll lim)
{
    memset(sta,0,sizeof(sta));memset(ned,0,sizeof(ned));memset(h,0,sizeof(h));memset(need,0,sizeof(need));
    atot=btot=ctot=0;
    for(int i=1;i<=m;i++)
    {
        ll x=query[i],cnt=0;
        for(int j=t;j>=0;j--) if(f[x][j]>1&&cnt+dist[x][j]<=lim){cnt+=dist[x][j];x=f[x][j];}
        if(f[x][0]==1&&cnt+dist[x][0]<=lim) h[++ctot]=make_pair(lim-cnt-dist[x][0],x);
        else sta[x]=1;
    }
    for(int i=head[1];i;i=Next[i]) if(!dfs(ver[i])) need[ver[i]]=1;
    sort(h+1,h+ctot+1);
    for(int i=1;i<=ctot;i++)
        if(need[h[i].second]&&h[i].first>=dist[h[i].second][0]) need[h[i].second]=0;
    for(int i=head[1];i;i=Next[i]) if(need[ver[i]]) return 0;
    return true;
}
int main()
{
    ll l=0,r=0,mid;cin>>n;t=log2(n)+1;
    for(int i=1;i<n;i++){int x,y,z;scanf("%d%d%d",&x,&y,&z);add(x,y,z);add(y,x,z);r+=z;}
    bfs();cin>>m;for(int i=1;i<=m;i++) scanf("%d",&query[i]);
    while(l<=r){mid=(l+r)>>1;if(check(mid)){r=mid-1;ans=mid;ok=1;}else l=mid+1;}
    if(!ok) cout<<-1;else cout<<ans;return 0;
}
