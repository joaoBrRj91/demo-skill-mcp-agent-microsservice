# Project Architecture Reference

> JL.Commerce.Tecnology.Service — all paths are relative to `JL.Commerce.Tecnology.Service/src/`.

```
src/
├── Domain/                                         ← zero external NuGet deps
│   ├── Abstractions/
│   │   └── AggregateRoot.cs                        ← base class for all aggregates
│   ├── Aggregates/
│   │   ├── CatalogProduct/
│   │   │   ├── CatalogProduct.cs                   ← canonical aggregate example
│   │   │   └── CatalogProductId.cs                 ← canonical strongly-typed ID example
│   │   └── Entity/
│   │       ├── Entity.cs
│   │       └── EntityId.cs
│   ├── Events/
│   │   ├── CatalogProductCreatedEvent.cs           ← canonical domain event example
│   │   └── EntityCreatedEvent.cs
│   ├── Exceptions/
│   │   ├── CatalogProductNotFoundException.cs
│   │   └── EntityNotFoundException.cs
│   └── JL.Commerce.Tecnology.Service.Domain.csproj ← must have 0 PackageReference items
│
├── Application/                                    ← orchestration only, no business logic
│   ├── Behaviors/
│   │   ├── LoggingBehavior.cs
│   │   └── ValidationBehavior.cs
│   ├── Commands/
│   │   ├── CreateCatalogProduct/                   ← canonical command example (3 files)
│   │   │   ├── CreateCatalogProductCommand.cs
│   │   │   ├── CreateCatalogProductCommandHandler.cs
│   │   │   └── CreateCatalogProductCommandValidator.cs
│   │   ├── DeleteCatalogProduct/
│   │   │   ├── DeleteCatalogProductCommand.cs
│   │   │   └── DeleteCatalogProductCommandHandler.cs
│   │   └── UpdateCatalogProduct/
│   │       ├── UpdateCatalogProductCommand.cs
│   │       ├── UpdateCatalogProductCommandHandler.cs
│   │       └── UpdateCatalogProductCommandValidator.cs
│   ├── DTOs/
│   │   ├── CatalogProductDto.cs
│   │   └── EntityDto.cs
│   ├── Mappings/
│   │   ├── CatalogProductMappingProfile.cs
│   │   └── EntityMappingProfile.cs
│   ├── Ports/                                      ← all interfaces live here
│   │   ├── ICatalogProductRepository.cs            ← canonical port example
│   │   ├── IEntityRepository.cs
│   │   └── IEventBus.cs
│   └── Queries/
│       ├── GetAllCatalogProducts/
│       │   ├── GetAllCatalogProductsQuery.cs
│       │   └── GetAllCatalogProductsQueryHandler.cs
│       └── GetCatalogProductById/                  ← canonical query example (2 files)
│           ├── GetCatalogProductByIdQuery.cs
│           └── GetCatalogProductByIdQueryHandler.cs
│
├── Infrastructure.Data/                            ← EF Core + PostgreSQL adapters
│   ├── Configurations/
│   │   ├── CatalogProductConfiguration.cs          ← canonical EF config example
│   │   └── EntityConfiguration.cs
│   ├── Context/
│   │   └── AppDbContext.cs
│   ├── Repositories/
│   │   ├── CatalogProductRepository.cs             ← canonical repository example
│   │   └── EntityRepository.cs
│   └── JL.Commerce.Tecnology.Service.Infrastructure.Data.csproj
│
├── Infrastructure.Integration/                     ← MassTransit 8.5.5 adapters
│   └── Messaging/
│       └── Consumers/
│           └── EntityCreatedConsumer.cs            ← canonical consumer example
│
└── Presentation/                                   ← Minimal API wiring
    ├── Endpoints/
    │   ├── CatalogProductEndpoints.cs              ← canonical endpoint example
    │   └── EntityEndpoints.cs
    └── Program.cs                                  ← DI composition root
```
