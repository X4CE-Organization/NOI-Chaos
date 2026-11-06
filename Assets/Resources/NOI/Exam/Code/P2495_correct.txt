#include <bits/stdc++.h>
#define INL inline
#define REG register
#define RPT(i,x,y) for (REG int i=x;i<y;i++)
#define DRPT(i,x,y) for (REG int i=x;i>y;i--)
#define MAXN 500500
#define LLINF 0x3f3f3f3f3f3f3f3f
using namespace std;

int dfn[MAXN], dep[MAXN], fa[MAXN][25], m[MAXN], lst[MAXN];
long long minv[MAXN];
bool query[MAXN];
int n,q,num,top,dfscnt=1,stak[MAXN];

struct EDGE { int to,next; long long val; } edge[MAXN<<1],edge1[MAXN<<1];
int head[MAXN],cnt=1,head1[MAXN],cnt1=1;
INL void add(int x,int y,long long v)
{
    edge[cnt].next=head[x]; edge[cnt].to=y; edge[cnt].val=v; head[x]=cnt++;
}
INL void add1(int x,int y)
{
    edge1[cnt1].next=head1[x]; edge1[cnt1].to=y; head1[x]=cnt1++;
}
void dfs(int pos)
{
    int k;
    for(k=0;fa[pos][k];k++) fa[pos][k+1]=fa[fa[pos][k]][k];
    m[pos]=k; dfn[pos]=dfscnt++;
    for(int i=head[pos];i;i=edge[i].next)
    {
        int to=edge[i].to;
        if(!dfn[to])
        {
            dep[to]=dep[pos]+1;
            minv[to]=min(minv[pos],edge[i].val);
            fa[to][0]=pos;
            dfs(to);
        }
    }
}
long long dfs1(int pos)
{
    long long sum=0;
    for(int i=head1[pos];i;i=edge1[i].next) sum+=dfs1(edge1[i].to);
    long long result=query[pos]?minv[pos]:min(minv[pos],sum);
    query[pos]=false;
    head1[pos]=0;
    return result;
}
int lca(int x,int y)
{
    if(dep[x]<dep[y]) swap(x,y);
    DRPT(i,m[x],-1) if(dep[fa[x][i]]>=dep[y]) x=fa[x][i];
    if(x==y) return x;
    DRPT(i,m[x],-1) if(fa[x][i]!=fa[y][i]) x=fa[x][i],y=fa[y][i];
    return fa[x][0];
}
bool cmp(int x1,int x2){ return dfn[x1]<dfn[x2]; }
int main()
{
    minv[1]=LLINF;
    cin>>n;
    int x,y; long long v;
    RPT(i,0,n-1){ scanf("%d%d%lld",&x,&y,&v); add(x,y,v); add(y,x,v); }
    dfs(1);
    cin>>q;
    while(q--)
    {
        cin>>num;
        RPT(i,1,num+1){ scanf("%d",&lst[i]); query[lst[i]]=true; }
        sort(lst+1,lst+num+1,cmp);
        stak[top=1]=lst[1];
        RPT(i,2,num+1)
        {
            int now=lst[i],lc=lca(now,stak[top]);
            while(1)
            {
                if(dep[lc]>=dep[stak[top-1]])
                {
                    if(lc!=stak[top])
                    {
                        add1(lc,stak[top]);
                        if(lc!=stak[top-1]) stak[top]=lc;
                        else top--;
                    }
                    break;
                }
                add1(stak[top-1],stak[top]);
                top--;
            }
            stak[++top]=now;
        }
        while(--top) add1(stak[top],stak[top+1]);
        cout<<dfs1(stak[1])<<endl;
        cnt1=1;
    }
    return 0;
}
