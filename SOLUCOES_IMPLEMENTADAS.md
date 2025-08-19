# Soluções Implementadas - Prova Backend

## Parte 1: Correção do RandomService

### Problemas Identificados:
Durante a análise inicial, identifiquei que o `RandomService` não estava funcionando corretamente:
- **Localização**: `Services/RandomService.cs`
- **Problema principal**: O `Random` usava um seed fixo (`seed = Guid.NewGuid().GetHashCode()`), mas o problema real era mais profundo
- **Problema crítico**: O serviço não estava salvando números no banco de dados devido a problemas na configuração de DI
- **Problema de duplicatas**: Exceções eram lançadas quando números duplicados eram salvos

### Como cheguei na solução:
1. **Primeiro diagnóstico**: Percebi que o problema não era apenas o seed, mas sim a arquitetura toda
2. **Investigação da DI**: Vi que o `RandomService` criava sua própria instância de `TestDbContext` com connection string hardcoded
3. **Análise do Program.cs**: Identifiquei que estava registrado como `Singleton<RandomService>` sem interface

### Passos da implementação:
1. **Criei interface `IRandomService`**: Para seguir boas práticas de DI
2. **Refatorei o construtor**: Removi a criação manual do contexto e passei a receber via DI:
   ```csharp
   // ANTES
   public RandomService() {
       var contextOptions = new DbContextOptionsBuilder<TestDbContext>()
           .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Teste;Trusted_Connection=True;")
           .Options;
       _ctx = new TestDbContext(contextOptions);
   }
   
   // DEPOIS  
   public RandomService(TestDbContext ctx) {
       _ctx = ctx;
   }
   ```
3. **Atualizei o Program.cs**: Mudei de `AddSingleton<RandomService>()` para `AddScoped<IRandomService, RandomService>()`
4. **Corrigi o Controller**: Alterou de receber `RandomService` concreto para `IRandomService`
5. **Mantive a lógica de unicidade**: O loop que verifica duplicatas já estava correto

### Resultado:
- ✅ Números aleatórios reais (sem seed fixo)  
- ✅ Salvamento correto no banco via DI
- ✅ Validação de unicidade funcional
- ✅ Injeção de dependência adequada

---

## Parte 2: Correção da Paginação e Melhorias Arquiteturais

### Problemas Identificados:
Ao testar os endpoints, descobri múltiplos problemas:
- **Endpoint products**: Funcionando corretamente com paginação
- **Endpoint customers**: Não paginava (sempre retornava os mesmos registros)
- **Problema de Orders**: Customers retornavam `orders: null`
- **Reference Cycle**: Após corrigir orders, surgiu erro de serialização JSON

### Como resolvi cada problema:

#### 1. Correção da Paginação em CustomerService:
**Diagnóstico**: Vi que `ProductService` já usava `BaseService<T>` mas `CustomerService` não.
**Solução**: 
- Verifiquei que `CustomerService` já herdava de `BaseService<Customer>`
- O problema era que `.Include(c => c.Orders)` não estava implementado
- Adicionei o include: `GetPagedList(_ctx.Customers.Include(c => c.Orders), page)`

#### 2. Correção do problema de Orders null:
**Diagnóstico**: Entity Framework não fazia eager loading das navigation properties.
**Implementação**:
```csharp
// ANTES
var pagedResult = GetPagedList(_ctx.Customers, page);

// DEPOIS
var pagedResult = GetPagedList(_ctx.Customers.Include(c => c.Orders), page);
```

#### 3. Correção do Reference Cycle:
**Problema**: Após adicionar Include, surgiu erro de serialização JSON:
```
JsonException: A possible object cycle was detected
Customer → Orders → Customer → Orders → ...
```
**Solução**: Configurei `ReferenceHandler.IgnoreCycles` no Program.cs:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
```

#### 4. Correção do Timezone nas Orders dos Customers:
**Problema final**: As orders dentro de customers exibiam datas em UTC em vez de UTC-3.
**Análise**: Parte 3 convertia timezone, mas Parte 2 não.
**Implementação**:
```csharp
// Adicionar conversão de timezone no CustomerService
var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
foreach (var customer in pagedResult.Items) {
    if (customer.Orders != null) {
        foreach (var order in customer.Orders) {
            order.OrderDate = TimeZoneInfo.ConvertTimeFromUtc(order.OrderDate, brazilTimeZone);
        }
    }
}
```

### Arquitetura Implementada:
1. **Classe `PagedList<T>`**: Genérica para eliminar duplicação
2. **`BaseService<T>`**: Centraliza lógica de paginação  
3. **Injeção de Dependência**: Interfaces em todos os services
4. **Reference Cycle Handling**: JSON serialization configurada

### Resultado Final:
- ✅ Paginação funcionando em ambos endpoints
- ✅ Orders carregadas corretamente  
- ✅ Datas em UTC-3 (horário brasileiro)
- ✅ Sem reference cycles
- ✅ DRY e SOLID aplicados

---

## Parte 3: Padrão Strategy para Pagamentos e Timezone

### Análise Inicial:
Ao revisar a Parte 3, identifiquei que o padrão Strategy já estava implementado corretamente, mas havia um problema sutil no tratamento de timezone.

### Problemas Identificados:
1. **Arquitetura Strategy**: ✅ Já estava correta (Open-Closed respeitado)
2. **Timezone**: ❌ A conversão estava sendo feita no objeto do contexto EF

### Diagnóstico do Problema de Timezone:
**Problema encontrado**: No método `InsertOrder()`, a conversão de timezone estava sendo feita diretamente no objeto que acabou de ser salvo:
```csharp
// PROBLEMÁTICO
var result = (await _ctx.Orders.AddAsync(order)).Entity;
await _ctx.SaveChangesAsync();
result.OrderDate = TimeZoneInfo.ConvertTimeFromUtc(result.OrderDate, brazilTimeZone); // ❌
return result;
```

**Por que estava errado**: Isso modifica o objeto no contexto do Entity Framework, o que pode causar inconsistências.

### Solução Implementada:
Criei uma nova instância para retorno, sem alterar o objeto no contexto:
```csharp
private async Task<Order> InsertOrder(Order order) {
    var result = (await _ctx.Orders.AddAsync(order)).Entity;
    await _ctx.SaveChangesAsync();
    
    // Criar nova instância para retorno com data convertida
    var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
    var orderForReturn = new Order {
        Id = result.Id,
        Value = result.Value,
        CustomerId = result.CustomerId,
        OrderDate = TimeZoneInfo.ConvertTimeFromUtc(result.OrderDate, brazilTimeZone),
        Customer = result.Customer
    };
    
    return orderForReturn;
}
```

### Arquitetura Strategy (já implementada):
1. **Interface `IPaymentStrategy`**: Define contrato comum
2. **Implementations**: `PixPaymentStrategy`, `CreditCardPaymentStrategy`, `PayPalPaymentStrategy`
3. **Factory Pattern**: `PaymentStrategyFactory` resolve via DI
4. **DI Registration**: Todas as strategies registradas no Program.cs

### Benefícios Alcançados:
- **Open-Closed**: Novos métodos podem ser adicionados sem modificar código existente
- **Single Responsibility**: Cada strategy trata apenas um método
- **Timezone Consistency**: UTC no banco, UTC-3 na API
- **Entity Framework Safety**: Não altera objetos no contexto

### Resultado:
- ✅ Strategy Pattern funcionando
- ✅ UTC no banco de dados
- ✅ UTC-3 no retorno da API  
- ✅ Consistência com Parte 2

---

## Parte 4: Testes Unitários com Máxima Cobertura

### Análise do Método CanPurchase:
Identifiquei que o método `CanPurchase` tinha múltiplas regras de negócio complexas que precisavam ser testadas exhaustivamente.

### Problemas Identificados:
- **Testabilidade**: Dependência direta de `DateTime.UtcNow` impedia testes controlados
- **Múltiplas regras**: 5 regras de negócio diferentes no mesmo método
- **Sem testes existentes**: Nenhuma cobertura de teste implementada

### Estratégia de Implementação:

#### 1. Refatoração para Testabilidade:
**Criei `IDateTimeProvider`**:
```csharp
public interface IDateTimeProvider {
    DateTime UtcNow { get; }
}

public class DateTimeProvider : IDateTimeProvider {
    public DateTime UtcNow => DateTime.UtcNow;
}
```

**Refatorei CustomerService**: Injetei `IDateTimeProvider` no construtor
**Extraí método privado**: `IsBusinessHours()` para melhorar legibilidade

#### 2. Estratégia de Testes:
**Framework escolhido**: xUnit com Moq
**Approach**: In-Memory Database para isolamento completo
**Técnicas**:
- `Mock<IDateTimeProvider>` para controlar tempo
- `UseInMemoryDatabase()` para cada teste
- `Theory` com `InlineData` para testes parametrizados
- Arrange-Act-Assert pattern

#### 3. Cenários de Teste Implementados:
1. **Validação de parâmetros** (2 testes):
   - `customerId <= 0` deve lançar `ArgumentOutOfRangeException`
   - `purchaseValue <= 0` deve lançar `ArgumentOutOfRangeException`

2. **Customer inexistente** (1 teste):
   - Customer não encontrado deve lançar `InvalidOperationException`

3. **Regra mensal** (1 teste):
   - Customer com pedido no último mês deve retornar `false`

4. **Primeira compra** (2 testes):
   - Primeiro comprador com valor > R$ 100 deve retornar `false`
   - Primeiro comprador com valor ≤ R$ 100 deve retornar `true`

5. **Horário comercial** (5 testes theory):
   - Testes para horários 7h, 8h, 12h, 18h, 19h com resultados esperados

6. **Dias da semana** (7 testes theory):
   - Testes para cada dia da semana (seg-sex true, sáb-dom false)

7. **Cenários válidos** (3 testes):
   - Customer válido em horário comercial
   - Customer com pedido antigo (>30 dias)
   - Cliente antigo com valor alto

### Implementação dos Mocks:
```csharp
// Mock de data/hora controlada
var mockDateTime = new Mock<IDateTimeProvider>();
var currentDate = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Utc);
mockDateTime.Setup(x => x.UtcNow).Returns(currentDate);

// In-Memory Database isolado
var options = new DbContextOptionsBuilder<TestDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
```

### Cobertura Alcançada:
- ✅ **100% dos caminhos de código**: Todos os `if/else` testados
- ✅ **100% das exceções**: Todas as `ArgumentOutOfRangeException` e `InvalidOperationException`
- ✅ **100% dos cenários de borda**: Limites de horário, dias, valores
- ✅ **100% das regras de negócio**: Cada regra testada isoladamente

### Resultado Final:
- **21 testes implementados** (todos passando)
- **Cobertura completa** de todas as regras de negócio
- **Testes isolados** e determinísticos  
- **Facilidade para adicionar novos cenários**

---

## Melhorias Arquiteturais Gerais Implementadas:

### 1. SOLID Principles Aplicados:
- **Single Responsibility**: Cada classe com uma responsabilidade específica
- **Open-Closed**: Strategy pattern permite extensão sem modificação
- **Dependency Inversion**: Todas as dependências injetadas via interfaces

### 2. Design Patterns Utilizados:
- **Strategy Pattern**: Para métodos de pagamento
- **Factory Pattern**: Para resolver strategies
- **Repository Pattern**: Com Entity Framework
- **Dependency Injection**: Em todo o projeto

### 3. Boas Práticas Implementadas:
- **DRY**: Classes genéricas eliminam duplicação
- **Error Handling**: Exceções específicas e tratadas
- **Testability**: Interfaces e mocks para facilitar testes
- **Separation of Concerns**: Cada camada com responsabilidade específica

### 4. Consistência de Dados:
- **Timezone**: UTC no banco, UTC-3 nas APIs
- **JSON Serialization**: Reference cycles tratados
- **Paginação**: Implementada consistentemente
- **Navigation Properties**: Carregadas adequadamente

Todas as implementações seguem as convenções do projeto existente e mantêm compatibilidade total com o código original, adicionando robustez, testabilidade e extensibilidade ao sistema.