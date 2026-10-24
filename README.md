# Oficina Mecânica API

**Tech Challenge FIAP — Fase 1** | API REST para gerenciamento de uma oficina mecânica.

[![Oficina Mecanica CI](https://github.com/davidnunesv/oficina-mecanica/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/davidnunesv/oficina-mecanica/actions/workflows/ci.yml)

O projeto contempla o cadastro de clientes, veículos, serviços e peças/insumos, além do ciclo de vida de ordens de serviço (OS): recebimento, diagnóstico, orçamento, aprovação, execução, finalização e entrega. Inclui autenticação JWT, autorização por perfil, persistência em SQL Server, testes automatizados e ambiente reproduzível com Docker Compose.

> **Ambiente de avaliação:** há endpoints de emissão de JWT exclusivos do modo de demonstração local, para permitir testar a API pelo Swagger sem configurar um provedor de identidade. Eles **não representam autenticação real de usuários** e não devem ser habilitados em produção.

## Tecnologias

- **.NET 10 / ASP.NET Core Web API** — endpoints REST.
- **C#**, **Clean Architecture** e conceitos de **Domain-Driven Design (DDD)**.
- **Entity Framework Core 10** e **SQL Server 2022** — persistência e migrations.
- **JWT Bearer** e políticas de autorização — perfis `Admin` e `Cliente`.
- **Swagger / OpenAPI (Swashbuckle)** — documentação e testes interativos.
- **Docker / Docker Compose** — API, banco e migrações.
- **xUnit**, `WebApplicationFactory` e **Coverlet** — testes e cobertura.
- **GitHub Actions** — integração contínua.

## Executar em poucos minutos (Docker)

### Pré-requisitos

- Git;
- Docker Desktop com Docker Compose v2, iniciado e com recursos suficientes para executar o SQL Server.

**1. Clonar a branch de desenvolvimento:**

```bash
git clone --branch dev https://github.com/davidnunesv/oficina-mecanica.git
cd oficina-mecanica
```

> A versão descrita neste README está na branch `dev`.

**2. Configurar a senha local do banco:**

No **PowerShell**:

```powershell
Copy-Item .env.example .env
notepad .env
```

No **Linux/macOS**:

```bash
cp .env.example .env
```

Defina uma senha forte e exclusiva para desenvolvimento no arquivo `.env`:

```dotenv
MSSQL_SA_PASSWORD=SUBSTITUA_POR_UMA_SENHA_FORTE
```

O `.env` é ignorado pelo Git. **Não use nem publique uma senha real no `.env.example`.**

**3. Iniciar o ambiente:**

```bash
docker compose up -d --build
```

**4. Verificar o resultado:**

```bash
docker compose ps -a
docker compose logs migrate
docker compose logs api
```

O esperado é encontrar `sqlserver` saudável, `migrate` concluído com código `0` e `api` em execução. O serviço `migrate` aplica automaticamente as migrations pendentes antes da inicialização da API.

**5. Abrir o Swagger:**

**http://localhost:8080/swagger**

| Serviço | Endereço / acesso |
| --- | --- |
| API e Swagger | `http://localhost:8080` / `/swagger` |
| SQL Server a partir do computador | `127.0.0.1,14333` |
| SQL Server dentro da rede Docker | `sqlserver,1433` |
| Banco da aplicação | `OficinaMecanica` |

O Compose expõe as portas apenas no `127.0.0.1`. O endpoint local utiliza **HTTP**, destinado à demonstração em máquina de desenvolvimento; não exponha esse ambiente à internet.

> **Atenção:** o `compose.yaml` usa `container_name: oficina-sqlserver` e portas fixas. Se já existir outra instância do projeto ativa, encerre-a antes para evitar conflitos. Use `docker compose down` para parar os serviços; **não use `docker compose down -v`**, pois a opção `-v` remove o volume persistente do banco.

## Autenticação de demonstração pelo Swagger

Com o Docker Compose, o modo de demonstração é habilitado pela combinação de `ASPNETCORE_ENVIRONMENT=Development` e `DemoAuth__Enabled=true`, configurados para a API. Ele permite gerar JWTs temporários **sem instalar `dotnet user-jwts` ou compartilhar User Secrets**.

### Administrador

1. Abra `http://localhost:8080/swagger`.
2. Expanda **`POST /dev/demo-token/admin`** e clique em **Try it out → Execute**.
3. Copie o valor de `accessToken` do JSON retornado.
4. Clique em **Authorize** no Swagger e cole **somente o token**, sem escrever `Bearer`.
5. Execute `GET /api/clientes`. Com o token Admin válido, a resposta esperada é `200 OK`.

### Cliente

1. Cadastre um cliente com o token Admin e copie o `id` retornado.
2. Execute **`POST /dev/demo-token/cliente/{clienteId}`**, substituindo o parâmetro pelo ID cadastrado.
3. Copie o novo `accessToken` e substitua o token atual em **Authorize**.
4. Consulte `GET /api/acompanhamento/{id}` com o ID de uma OS pertencente àquele cliente.

O token de cliente pode acompanhar **a própria OS**, mas não acessar endpoints administrativos. O recurso retorna `404` para uma OS de outro proprietário, sem revelar sua existência.

| Situação | Resultado esperado |
| --- | --- |
| Endpoint administrativo sem JWT | `401 Unauthorized` |
| Endpoint administrativo com JWT Cliente | `403 Forbidden` |
| Endpoint administrativo com JWT Admin | Sucesso conforme a operação |
| Acompanhamento da OS pelo proprietário | `200 OK` |
| Acompanhamento da OS por outro cliente | `404 Not Found` |
| JWT inválido ou adulterado | `401 Unauthorized` |

**Segurança:** nesse modo, os endpoints de demonstração emitem JWTs **sem verificar identidade ou senha**. Eles só devem ser utilizados em ambiente local confiável. A chave de assinatura é gerada aleatoriamente na inicialização da API; ao reiniciar o container, gere novos tokens. Os tokens possuem validade de **1 hora**. Para produção, o sistema requer integração com um provedor de identidade confiável e desativação explícita do modo de demonstração.

## Funcionalidades e endpoints

| Área | Operações principais | Permissão |
| --- | --- | --- |
| Clientes | `GET`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` em `/api/clientes` | Admin |
| Veículos | CRUD em `/api/veiculos` | Admin |
| Catálogo de serviços | CRUD em `/api/servicos` | Admin |
| Peças e insumos | CRUD em `/api/pecas-insumos` | Admin |
| Ordens de serviço | Criação, consulta, diagnóstico, itens, orçamento e entrega em `/api/ordens-servico` | Admin |
| Relatório | `GET /api/ordens-servico/relatorios/tempo-medio` | Admin |
| Acompanhamento | `GET /api/acompanhamento/{id}` | Cliente proprietário |
| Tokens locais | `POST /dev/demo-token/admin` e `POST /dev/demo-token/cliente/{clienteId}` | Apenas demonstração local |

O Swagger apresenta a descrição completa dos métodos, parâmetros e contratos de requisição.

## Cenário de teste: manutenção completa

Com um token **Admin** autorizado no Swagger, execute os passos abaixo em ordem. Anote os GUIDs retornados nas operações de cadastro e substitua os valores indicados entre `<>`.

**1. Cadastrar cliente** — `POST /api/clientes`

```json
{
  "nome": "João Silva",
  "documento": "52998224725"
}
```

**2. Cadastrar veículo** — `POST /api/veiculos`

```json
{
  "clienteId": "<GUID_CLIENTE>",
  "placa": "ABC1D23",
  "marca": "Volkswagen",
  "modelo": "Gol",
  "ano": 2020
}
```

**3. Cadastrar serviço** — `POST /api/servicos`

```json
{
  "nome": "Troca de óleo",
  "descricao": "Substituição do óleo do motor",
  "preco": 150.00
}
```

**4. Cadastrar peça** — `POST /api/pecas-insumos`

```json
{
  "nome": "Filtro de óleo",
  "preco": 45.90
}
```

**5. Abrir OS** — `POST /api/ordens-servico`

```json
{
  "clienteId": "<GUID_CLIENTE>",
  "veiculoId": "<GUID_VEICULO>"
}
```

**6. Iniciar diagnóstico** — `POST /api/ordens-servico/{osId}/diagnostico/iniciar`, sem corpo.

**7. Registrar diagnóstico** — `POST /api/ordens-servico/{osId}/diagnostico`

```json
{
  "descricao": "Necessária troca de óleo e substituição do filtro."
}
```

**8. Adicionar serviço** — `POST /api/ordens-servico/{osId}/servicos`

```json
{
  "servicoId": "<GUID_SERVICO>",
  "quantidade": 1
}
```

**9. Adicionar peça** — `POST /api/ordens-servico/{osId}/pecas`

```json
{
  "pecaId": "<GUID_PECA>"
}
```

**10. Gerar orçamento** — `POST /api/ordens-servico/{osId}/orcamento`.

**11. Consultar detalhes** — `GET /api/ordens-servico/{osId}/detalhes`. O total esperado para este exemplo é **R$ 195,90** (R$ 150,00 pelo serviço + R$ 45,90 pela peça).

**12. Aprovar orçamento** — `POST /api/ordens-servico/{osId}/orcamento/aprovar`. A OS entra em execução e a peça é reservada para essa OS.

**13. Finalizar a OS** — `POST /api/ordens-servico/{osId}/finalizar`. A peça reservada é consumida.

**14. Entregar o veículo** — `POST /api/ordens-servico/{osId}/entregar`.

**15. Consultar novamente** — `GET /api/ordens-servico/{osId}/detalhes`. O status esperado é **`Entregue`**.

As alterações de estado bem-sucedidas retornam, em geral, `204 No Content`; os cadastros retornam `201 Created`.

### Estados da Ordem de Serviço

```text
Recebida
   ↓
EmDiagnostico
   ↓
AguardandoAprovacao
   ↓ (orçamento aprovado)
EmExecucao
   ↓
Finalizada
   ↓
Entregue
```

A rejeição do orçamento também está disponível em `POST /api/ordens-servico/{osId}/orcamento/rejeitar`. As regras de transição de estados são validadas pelo domínio.

## Arquitetura

O projeto organiza responsabilidades em camadas, com o domínio de negócio isolado dos detalhes de HTTP e persistência:

```text
src/
├── Oficina.Api/             # Controllers, contratos HTTP, JWT e Swagger
├── Oficina.Application/     # Casos de uso, serviços, DTOs e interfaces
├── Oficina.Domain/          # Entidades, enums, value objects e regras de negócio
├── Oficina.Infrastructure/  # EF Core, SQL Server, repositórios e transações
└── Oficina.DbMigrator/      # Aplicação independente para aplicar migrations

tests/
├── Oficina.UnitTests/       # Testes unitários de negócio
└── Oficina.IntegrationTests/# Testes com SQL Server e endpoints HTTP
```

Princípios aplicados:

- **Domain:** concentra estados e invariantes das ordens, orçamentos, peças e outras entidades.
- **Application:** orquestra operações e utiliza abstrações de repositório e transação.
- **Infrastructure:** implementa acesso ao SQL Server com EF Core e repositórios.
- **API:** expõe os casos de uso via REST, valida requests, autentica e autoriza chamadas.
- **DbMigrator:** atualiza o schema antes de iniciar a API no Compose.

A aprovação do orçamento reserva peças; a finalização consome as peças reservadas. Essas operações são executadas de forma transacional.

## Testes, cobertura e CI

O projeto usa **xUnit** para testes unitários, de integração com SQL Server e de endpoints HTTP (incluindo autenticação e autorização JWT).

Na última execução verificada do GitHub Actions na branch `dev` (**24/10/2026**), foram registrados:

| Indicador | Resultado |
| --- | ---: |
| Testes unitários | 291 aprovados |
| Testes de integração | 34 aprovados |
| **Total** | **325 aprovados; 0 falhas** |
| Cobertura de linhas — Domain + Application | **98,6%** |
| Cobertura de branches — Domain + Application | **90,4%** |
| Cobertura de linhas — Application | **99,5%** |
| Cobertura de linhas — Domain | **97,4%** |

A cobertura acima refere-se às assemblies `Oficina.Domain` e `Oficina.Application` filtradas em `coverage.runsettings`; **não representa a cobertura global de API + Infrastructure**. Os percentuais são históricos e podem variar após alterações no projeto. O pipeline coleta e publica os relatórios, mas não aplica automaticamente um limite mínimo de cobertura como condição de aprovação.

**Pipeline:** [GitHub Actions — Oficina Mecânica](https://github.com/davidnunesv/oficina-mecanica/actions/workflows/ci.yml). Disparado em pushes e Pull Requests para `dev` e `main`, com restore, build, testes, cobertura e build da imagem Docker.

### Executar testes localmente

Para os testes de integração, é necessário ter o SQL Server local na porta `14333` e definir a variável `OFICINA_TEST_CONNECTION_STRING` apontando **exclusivamente** para o banco `OficinaMecanica_IntegrationTests`.

Exemplo no PowerShell (substitua a senha pela configurada no seu `.env`):

```powershell
$env:OFICINA_TEST_CONNECTION_STRING = "Server=127.0.0.1,14333;Database=OficinaMecanica_IntegrationTests;User Id=sa;Password=SUA_SENHA;Encrypt=True;TrustServerCertificate=True;"
dotnet test OficinaMecanica.slnx
```

Para gerar cobertura:

```powershell
dotnet test OficinaMecanica.slnx --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Os testes de integração executam migrations no **banco de testes separado**, nunca no banco de desenvolvimento do exemplo. A execução local da suíte exige o SDK .NET 10; **para avaliar a API pelo Swagger, apenas Git e Docker são suficientes**.

## Comandos úteis e solução de problemas

```bash
docker compose ps -a          # Estado dos serviços
docker compose logs migrate   # Logs das migrations
docker compose logs api       # Logs da API
docker compose logs sqlserver # Logs do SQL Server
docker compose down           # Parar serviços sem apagar o volume
docker compose up -d --build  # Reconstruir e iniciar o ambiente
```

| Problema | Verificação recomendada |
| --- | --- |
| Swagger indisponível | Confira `docker compose ps -a` e `docker compose logs api` |
| SQL Server não fica saudável | Confira senha forte do SA, Docker Desktop, recursos disponíveis e `docker compose logs sqlserver` |
| Migrador falha | Examine `docker compose logs migrate` e a conexão interna `sqlserver,1433` |
| Endpoints retornam `401` | Gere um novo token de demonstração, autorize no Swagger e verifique se a API foi reiniciada |
| Endpoints retornam `403` | Confira a role do token (`Admin` ou `Cliente`) |
| Conflito de portas/containers | Pare outra execução do mesmo projeto antes de subir o Compose |

## Observações de segurança e escopo

- O modo de demonstração usa endpoints públicos de emissão de token, **apenas para experimentação local**. Não substitui login, cadastro de usuários ou verificação de identidade real.
- Para produção: desabilitar `DemoAuth`, integrar um provedor de identidade confiável, fornecer configuração JWT segura, usar HTTPS, proteger segredos e criar credenciais SQL com privilégios mínimos.
- A aplicação de migrations via container foi projetada para o ambiente de demonstração; implantações de produção devem usar um processo de atualização de schema controlado.
- Não versionar `.env`, chaves privadas, User Secrets ou dados reais de clientes.

---

**Repositório:** https://github.com/davidnunesv/oficina-mecanica · **Branch de desenvolvimento:** `dev`
