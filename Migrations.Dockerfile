FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY ./publish-migrations .
ENTRYPOINT ["dotnet", "EPCLeadGenerator.Database.dll"]