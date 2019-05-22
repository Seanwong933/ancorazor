# Docker file of Blog.API

# 1. 将 siegrainwong/aspnetcore-build 作为运行环境（包含2.2的sdk和nodejs），运行目录为app目录，并暴露端口9020
FROM siegrainwong/aspnetcore-build:2.2 AS base
WORKDIR /app
EXPOSE 80

# 2. 将 siegrainwong/aspnetcore-build 作为编译环境，编译目录为src
# 然后是复制命令，这里要注意的是，此前我们已经在docker-compose.yml中设置了工作上下文为service/tasklist，所以第一个.的目录就在service/tasklist，而第二个.就是容器的工作目录/src
# 接着就是dotnet restore命令，然后进入目录，执行publish命令，并将编译结果输出到/app目录。
FROM siegrainwong/aspnetcore-build:2.2 AS publish
WORKDIR /src
COPY . .
RUN dotnet restore
WORKDIR /src/Blog.API
RUN dotnet publish "Blog.API.csproj" -c Release -o /app

# 3. 进入第一部分（FROM base）的app目录，复制第二部分（--from=publish）的app目录中的编译文件到第一部分的/app目录下
# 然后跑起来
FROM base AS final
WORKDIR /app
COPY --from=publish /app .
CMD ["dotnet", "Blog.API.dll"]