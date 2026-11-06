#include<bits/stdc++.h>
#define fp(i,a,b) for(register int i=a,I=b+1;i<I;++i)
#define fd(i,a,b) for(register int i=a,I=b-1;i>I;--i)
#define go(u) for(register int i=fi[u],v=e[i].to;i;v=e[i=e[i].nx].to)
using namespace std;
char ss[1<<17],*A=ss,*B=ss;
inline char gc(){return A==B&&(B=(A=ss)+fread(ss,1,1<<17,stdin),A==B)?-1:*A++;}
template<class T>inline void sd(T&x){char c;T y=1;while(c=gc(),(c<48||57<c)&&c!=-1)if(c==45)y=-1;x=c-48;while(c=gc(),47<c&&c<58)x=x*10+c-48;x*=y;}
char sr[1<<21],z[20];int C=-1,Z;
inline void Ot(){fwrite(sr,1,C+1,stdout),C=-1;}
template<class T>inline void we(T x){if(C>1<<20)Ot();if(x<0)sr[++C]=45,x=-x;while(z[++Z]=x%10+48,x/=10);while(sr[++C]=z[Z],--Z);sr[++C]='\n';}
const int N=1e5+5,P=998244353;
typedef int arr[N];
struct eg{int nx,to;}e[N<<1];
int n,m,ans,dft,Cnt,f[N][2],fg[N][2];arr fi,fa,eu,ev,Mi,dfn,vis;
inline int pls(int a,int b){return a+=b,a<P?a:a-P;}
void dfs(int u)
{
    dfn[u]=++dft;
    go(u)if(v^fa[u]){if(!dfn[v])fa[v]=u,dfs(v);else if(dfn[u]<dfn[v])eu[++Cnt]=u,ev[Cnt]=v;}
}
void dp(int u)
{
    f[u][0]=!fg[u][1],f[u][1]=!fg[u][0],vis[u]=1;
    go(u)if(!vis[v]){dp(v);f[u][1]=(long long)f[u][1]*f[v][0]%P;f[u][0]=(long long)f[u][0]*(f[v][0]+f[v][1])%P;}
}
inline void add(int u,int v){static int ce=0;e[++ce]={fi[u],v},fi[u]=ce;}
int main()
{
    sd(n),sd(m);int u,v;
    fp(i,1,m)sd(u),sd(v),add(u,v),add(v,u);
    dfs(1);Mi[1]=1;fp(i,2,Cnt)Mi[i]=Mi[i-1]<<1;
    fp(s,0,(1<<Cnt)-1)
    {
        fp(i,1,Cnt)if(s&Mi[i])fg[eu[i]][1]=fg[ev[i]][0]=1;else fg[eu[i]][0]=1;
        fp(i,1,n)vis[i]=0;dp(1);ans=pls(ans,pls(f[1][1],f[1][0]));
        fp(i,1,Cnt)if(s&Mi[i])fg[eu[i]][1]=fg[ev[i]][0]=0;else fg[eu[i]][0]=0;
    }
    we(ans);
    Ot();
    return 0;
}
