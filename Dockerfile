# Docker file of Blog.API

# 1. �� siegrainwong/aspnetcore-build ��Ϊ���л���������2.2��sdk��nodejs��������Ŀ¼ΪappĿ¼������¶�˿�9020
FROM siegrainwong/aspnetcore-build:2.2 AS base
WORKDIR /app
EXPOSE 80

# 2. �� siegrainwong/aspnetcore-build ��Ϊ���뻷��������Ŀ¼Ϊsrc
# Ȼ���Ǹ����������Ҫע����ǣ���ǰ�����Ѿ���docker-compose.yml�������˹���������Ϊservice/tasklist�����Ե�һ��.��Ŀ¼����service/tasklist�����ڶ���.���������Ĺ���Ŀ¼/src
# ���ž���dotnet restore���Ȼ�����Ŀ¼��ִ��publish������������������/appĿ¼��
FROM siegrainwong/aspnetcore-build:2.2 AS publish
WORKDIR /src
COPY . .
RUN dotnet restore
WORKDIR /src/Blog.API
RUN dotnet publish "Blog.API.csproj" -c Release -o /app

# 3. �����һ���֣�FROM base����appĿ¼�����Ƶڶ����֣�--from=publish����appĿ¼�еı����ļ�����һ���ֵ�/appĿ¼��
# Ȼ��������
FROM base AS final
WORKDIR /app
COPY --from=publish /app .
CMD ["dotnet", "Blog.API.dll"]