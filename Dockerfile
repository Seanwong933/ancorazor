# Docker file of Blog.API

# 1. �� siegrainwong/aspnetcore-build ��Ϊ���л���������2.2��sdk��nodejs��������Ŀ¼ΪappĿ¼������¶�˿�8088
FROM siegrainwong/aspnetcore-build:2.2 AS base
WORKDIR /app
EXPOSE 8088

# 2. �� siegrainwong/aspnetcore-build ��Ϊ���뻷��������Ŀ¼Ϊsrc
# Ȼ���Ǹ����������Ҫע����ǣ���ǰ�����Ѿ���docker-compose.yml�������˹���������Ϊservice/tasklist�����Ե�һ��.��Ŀ¼����service/tasklist�����ڶ���.���������Ĺ���Ŀ¼/src
# ���ž���dotnet restore���Ȼ�����Ŀ¼��ִ��publish������������������/appĿ¼��
FROM siegrainwong/aspnetcore-build:2.2 AS publish
WORKDIR /src
COPY . .
WORKDIR /src/Blog.API
RUN dotnet publish "Blog.API.csproj" -c Release -o /app
WORKDIR /app
CMD ["dotnet", "Blog.API.dll"]