#See https://aka.ms/customizecontainer to learn more about how VS uses this Dockerfile to build images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN ln -sf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime

COPY . .
ENTRYPOINT ["dotnet", "IotWebApi.dll"]
