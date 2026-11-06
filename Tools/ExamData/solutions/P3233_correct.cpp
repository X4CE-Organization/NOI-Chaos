#include <bits/stdc++.h>
using namespace std;
const int N=3e5+10,INF=1e9+10;
int n,m,top,cnt;
int dep[N],f[N][20],dfn[N],size[N],lg2[N],sta[N],pnt[N],vis[N],g[N],dp[N],ans[N],tmp[N];
struct graph
{
    int tot;
    int fir[N],to[2*N],nxt[2*N];
    graph(){ tot=0; memset(fir,0,sizeof(fir)); }
    inline void add(int x,int y)
    {
        to[++tot]=y; nxt[tot]=fir[x]; fir[x]=tot;
        to[++tot]=x; nxt[tot]=fir[y]; fir[y]=tot;
    }
}e1,e2;
inline void dfs(int p)
{
    dfn[p]=++cnt,size[p]=1;
    for(register int i=e1.fir[p];i;i=e1.nxt[i])
    {
        int q=e1.to[i];
        if(q==f[p][0]) continue;
        dep[q]=dep[p]+1,f[q][0]=p;
        for(register int j=1;j<=lg2[dep[q]]+1;j++)
            f[q][j]=f[f[q][j-1]][j-1];
        dfs(q);
        size[p]+=size[q];
    }
}
inline int get_lca(int x,int y)
{
    if(dep[x]<dep[y]) swap(x,y);
    for(register int i=lg2[dep[x]];i>=0;i--)
        if(dep[f[x][i]]>=dep[y]) x=f[x][i];
    if(x==y) return x;
    for(register int i=lg2[dep[x]];i>=0;i--)
        if(f[x][i]!=f[y][i]) x=f[x][i],y=f[y][i];
    return f[x][0];
}
inline bool cmp(int a,int b){ return dfn[a]<dfn[b]; }
inline void build(int p)
{
    if(top==0){ sta[top=1]=p; return; }
    int lca=get_lca(sta[top],p);
    while(top>1&&dep[lca]<dep[sta[top-1]]) e2.add(sta[top-1],sta[top]),top--;
    if(dep[lca]<dep[sta[top]]) e2.add(lca,sta[top--]);
    if(top==0||sta[top]!=lca) sta[++top]=lca;
    sta[++top]=p;
}
inline void cal(int x,int y)
{
    int p=y,q=y;
    for(register int i=lg2[dep[p]];i>=0;i--)
        if(dep[f[p][i]]>dep[x]) p=f[p][i];
    ans[g[x]]-=size[p];
    for(register int i=lg2[dep[q]];i>=0;i--)
    {
        int llen=dep[y]-dep[f[q][i]]+dp[y],rlen=dep[f[q][i]]-dep[x]+dp[x];
        if(dep[f[q][i]]>dep[x]&&(llen<rlen||(llen==rlen&&g[y]<g[x]))) q=f[q][i];
    }
    ans[g[y]]+=size[q]-size[y],ans[g[x]]+=size[p]-size[q];
}
inline void dfs1(int p,int fa)
{
    dp[p]=INF;
    for(register int i=e2.fir[p];i;i=e2.nxt[i])
    {
        int q=e2.to[i];
        if(q==fa) continue;
        dfs1(q,p);
        int dis=dep[q]-dep[p];
        if(dp[q]+dis<dp[p]) dp[p]=dp[q]+dis,g[p]=g[q];
        else if(dp[q]+dis==dp[p]) g[p]=min(g[p],g[q]);
    }
    if(vis[p]) dp[p]=0,g[p]=p;
}
inline void dfs2(int p,int fa)
{
    for(register int i=e2.fir[p];i;i=e2.nxt[i])
    {
        int q=e2.to[i];
        if(q==fa) continue;
        int dis=dep[q]-dep[p];
        if(dp[p]+dis<dp[q]) dp[q]=dp[p]+dis,g[q]=g[p];
        else if(dp[p]+dis==dp[q]) g[q]=min(g[q],g[p]);
        cal(p,q);
        dfs2(q,p);
    }
    ans[g[p]]+=size[p];
    vis[p]=e2.fir[p]=0;
}
int main()
{
    lg2[1]=0;
    for(register int i=1;i<=3e5;i++) lg2[i]=lg2[i>>1]+1;
    int a,b,T;
    scanf("%d",&n);
    for(register int i=1;i<n;i++)
    {
        scanf("%d%d",&a,&b);
        e1.add(a,b);
    }
    dep[1]=1,dfs(1);
    scanf("%d",&T);
    while(T--)
    {
        int flag=1;
        top=e2.tot=0;
        scanf("%d",&m);
        for(register int i=1;i<=m;i++)
            scanf("%d",&pnt[i]),vis[pnt[i]]=1,ans[pnt[i]]=0;
        if(!vis[1]) pnt[++m]=1,flag=0;
        for(register int i=1;i<=m;i++) tmp[i]=pnt[i];
        sort(pnt+1,pnt+m+1,cmp);
        for(register int i=1;i<=m;i++) build(pnt[i]);
        if(top) while(--top) e2.add(sta[top],sta[top+1]);
        dfs1(1,0),dfs2(1,0);
        for(register int i=1;i<=m;i++)
            if(tmp[i]!=1||flag) printf("%d ",ans[tmp[i]]);
        printf("\n");
    }
    return 0;
}
