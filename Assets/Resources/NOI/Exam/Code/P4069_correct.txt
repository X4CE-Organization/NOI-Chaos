#include<bits/stdc++.h>
using namespace std;
#define db double
#define re register int
#define ll long long
#define inf 123456789123456789ll

char qwq;
inline char getch()
{
    static char buf[10000],*p1=buf,*p2=buf;
    return p1==p2&&(p2=(p1=buf)+fread(buf,1,10000,stdin),p1==p2)?EOF:*p1++;
}
inline int read()
{
    re lf=0,ioi=1;qwq=getch();
    while(qwq<'0'||qwq>'9') ioi=qwq=='-'?~ioi+1:1,qwq=getch();
    while(qwq>='0'&&qwq<='9') lf=(lf<<3)+(lf<<1)+(qwq^48),qwq=getch();
    return lf*ioi;
}
#define ls(x) (x<<1)
#define rs(x) (x<<1|1)
int n,m,tot,h[100005],cnt,dep[100005],top[100005],son[100005];
int size[100005],id[100005],bel[100005],fa[100005];
ll k[400005],b[400005],mn[400005],t[400005],dis[100005];
struct did{int next,to,w;}e[200005];
inline void add(re x,re y,re z)
{
    e[++cnt]=(did){h[x],y,z},h[x]=cnt;
    e[++cnt]=(did){h[y],x,z},h[y]=cnt;
}
void build(re p,re l,re r)
{
    mn[p]=inf;t[p]=1;
    if(l==r) return;
    re mid=(l+r)>>1;
    build(ls(p),l,mid);build(rs(p),mid+1,r);
}
inline ll cal(re x,re id) {return k[id]*dis[bel[x]]+b[id];}
inline void push_up(re x) {mn[x]=min(mn[x],min(mn[ls(x)],mn[rs(x)]));}
void update(re nl,re nr,re p,re l,re r,re x)
{
    re mid=(l+r)>>1;
    if(nl<=l&&r<=nr)
    {
        if(cal(l,x)<=cal(l,t[p])&&cal(r,x)<=cal(r,t[p]))
        {
            t[p]=x,mn[p]=min(mn[p],min(cal(l,x),cal(r,x)));
            return;
        }
        if(cal(l,x)>=cal(l,t[p])&&cal(r,x)>=cal(r,t[p])) return;
        if(k[x]<k[t[p]])
        {
            if(cal(mid,x)<=cal(mid,t[p])) update(nl,nr,ls(p),l,mid,t[p]),t[p]=x;
            else update(nl,nr,rs(p),mid+1,r,x);
        }
        else
        {
            if(cal(mid,x)<=cal(mid,t[p])) update(nl,nr,rs(p),mid+1,r,t[p]),t[p]=x;
            else update(nl,nr,ls(p),l,mid,x);
        }
        return mn[p]=min(mn[p],min(cal(l,x),cal(r,x))),push_up(p),void();
    }
    if(nl<=mid) update(nl,nr,ls(p),l,mid,x);
    if(nr>mid) update(nl,nr,rs(p),mid+1,r,x);
    push_up(p);
}
ll query(re ql,re qr,re p,re l,re r)
{
    if(ql<=l&&r<=qr) return mn[p];
    re mid=(l+r)>>1;ll res=inf;
    if(b[t[p]]!=inf) res=min(cal(max(l,ql),t[p]),cal(min(r,qr),t[p]));
    if(ql<=mid) res=min(res,query(ql,qr,ls(p),l,mid));
    if(mid<qr) res=min(res,query(ql,qr,rs(p),mid+1,r));
    return res;
}
void dfs1(re u,re prt)
{
    fa[u]=prt,dep[u]=dep[prt]+1,size[u]=1;
    for(re i=h[u],v;v=e[i].to,i;i=e[i].next)
    {
        if(v==prt) continue;
        dis[v]=dis[u]+e[i].w;dfs1(v,u);size[u]+=size[v];
        if(size[v]>size[son[u]]) son[u]=v;
    }
}
void dfs2(re u,re tp)
{
    top[u]=tp,bel[id[u]=++id[0]]=u;
    if(son[u]) dfs2(son[u],tp);
    for(re i=h[u],v;v=e[i].to,i;i=e[i].next)
        if(v!=fa[u]&&v!=son[u]) dfs2(v,v);
}
inline int lca(re u,re v)
{
    while(top[u]!=top[v]) dep[top[u]]>dep[top[v]]?u=fa[top[u]]:v=fa[top[v]];
    return dep[u]>dep[v]?v:u;
}
inline void updrange(re u,re v)
{
    while(top[u]!=top[v]) update(id[top[u]],id[u],1,1,n,tot),u=fa[top[u]];
    update(id[v],id[u],1,1,n,tot);
}
inline ll ask(re u,re v)
{
    ll ans=inf;
    while(top[u]!=top[v])
    {
        if(dep[top[u]]<dep[top[v]]) swap(u,v);
        ans=min(ans,query(id[top[u]],id[u],1,1,n));
        u=fa[top[u]];
    }
    if(dep[u]>dep[v]) swap(u,v);
    return min(ans,query(id[u],id[v],1,1,n));
}
int main()
{
    n=read(),m=read();
    for(re i=1;i<n;i++)
    {
        re a=read(),b=read(),c=read();
        add(a,b,c);
    }
    dfs1(1,0);dfs2(1,1);
    k[++tot]=0,b[tot]=inf;build(1,1,n);
    while(m--)
    {
        ll op=read(),s=read(),t=read(),w=lca(s,t),x,y;
        if(op==1)
        {
            x=read(),y=read();
            k[++tot]=-x,b[tot]=x*dis[s]+y;updrange(s,w);
            k[++tot]=x,b[tot]=x*(dis[s]-(dis[w]<<1))+y;updrange(t,w);
        }
        else printf("%lld\n",ask(s,t));
    }
    return 0;
}
