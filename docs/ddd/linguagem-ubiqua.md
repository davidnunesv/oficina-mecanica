# Linguagem Ubíqua — Oficina Mecânica

**Tech Challenge FIAP — Fase 1**

## 1. Objetivo

Este documento consolida o vocabulário compartilhado entre negócio, Event Storming, diagramas C4, código e testes. O domínio principal é o atendimento de uma oficina mecânica, desde a abertura da **Ordem de Serviço (OS)** até a entrega do veículo.

A solução da Fase 1 é uma API REST em **ASP.NET Core (.NET 10)**, organizada em projetos de API, Application, Domain e Infrastructure seguindo o Clean Architecture e persistida em **SQL Server.**

## 2. Atores e papéis

| Termo | Significado no negócio | Autorização na aplicação |
| --- | --- | --- |
| **Cliente** | Proprietário/solicitante do atendimento associado à OS. | Role `Cliente`: consulta o andamento da própria OS e **aprova ou rejeita o orçamento da própria OS**. |
| **Atendente** | Recebe o veículo, realiza cadastros, abre OS, gera orçamento e participa da entrega. | No MVP utiliza operações protegidas por `AdminOnly`. |
| **Mecânico** | Registra diagnóstico, adiciona serviços, peças e executa manutenção. | No MVP utiliza operações protegidas por `AdminOnly`. |
| **Estoquista** | Administra peças/insumos e acompanha disponibilidade de cada unidade. | No MVP utiliza operações protegidas por `AdminOnly`. |

**Distinção importante:** `Atendente`, `Mecânico` e `Estoquista` são papéis do processo de negócio. A API implementa, para autorização, apenas `Admin` e `Cliente`; não há uma role técnica para cada profissão.

## 3. Glossário do domínio

| Termo preferencial | Definição compartilhada | Correspondência técnica |
| --- | --- | --- |
| **Cliente** | Cadastro da pessoa física ou jurídica associada aos atendimentos. | `Cliente` |
| **Documento** | CPF/CNPJ validado usado para identificar um cliente. | Value Object `Documento` |
| **Veículo** | Automóvel atendido pela oficina e vinculado a um cliente, com placa, marca, modelo e ano. | `Veiculo` |
| **Placa** | Identificação do veículo validada pelo domínio. | Value Object `Placa` |
| **Serviço** | Atividade de manutenção ofertada pela oficina, com nome, descrição e preço de catálogo. | `Servico` |
| **Peça/Insumo** | **Unidade individual** cadastrada de peça ou insumo, identificada por ID próprio. | `PecaInsumo` |
| **Estoque** | Controle da disponibilidade e do vínculo de cada unidade, **sem saldo numérico agregado**. | `PecaInsumo`, `StatusPecaInsumo` |
| **Ordem de Serviço (OS)** | Registro central do atendimento: cliente, veículo, diagnóstico, itens, orçamento e progresso. | Raiz do agregado `OrdemServico` |
| **Diagnóstico** | Registro técnico das necessidades de manutenção de uma OS. | `Diagnostico` |
| **Item de Serviço** | Serviço incluído na OS com quantidade e preço unitário aplicado. | `ItemServico` |
| **Item de Peça** | Referência a uma unidade de peça incluída na OS e seu preço aplicado. | `ItemPecaOS` |
| **Orçamento** | Entidade **interna à OS**, calculada a partir dos itens da manutenção. | `Orcamento` |
| **Preço aplicado** | Valor congelado no item da OS ao ser incluído, independente de alteração posterior do catálogo. | `PrecoAplicado` dos itens |
| **Geração de orçamento** | Cálculo dos valores e mudança da OS para aguardar decisão. | `OrdemServico.GerarOrcamento()` |
| **Aprovação do orçamento** | Decisão **do cliente proprietário**, sobre orçamento pendente; reserva peças e inicia execução. | `AprovarOrcamentoUseCase`, `OrdemServico.AprovarOrcamento()` |
| **Rejeição do orçamento** | Decisão **do cliente proprietário**, sobre orçamento pendente; impede iniciar a execução. | `OrdemServicoApplicationService.RejeitarOrcamentoAsync()`, `OrdemServico.RejeitarOrcamento()` |
| **Reserva de peça** | Associação exclusiva de uma unidade disponível à OS cujo orçamento foi aprovado. | `PecaInsumo.Reservar(osId)` |
| **Consumo de peça** | Mudança de estado da unidade reservada para consumida, durante a finalização. | `PecaInsumo.Consumir(osId)` |
| **Acompanhamento da OS** | Consulta de leitura do estado atual da OS pelo respectivo cliente. | `GET /api/acompanhamento/{id}` |
| **Tempo médio de execução** | Média entre início de execução e finalização das OS concluídas. | `GetTempoMedioExecucaoAsync()` |

## 4. Ciclo de vida da Ordem de Serviço

`Recebida → EmDiagnostico → AguardandoAprovacao → EmExecucao → Finalizada → Entregue`

| Estado da OS | Significado | Próximo passo permitido |
| --- | --- | --- |
| `Recebida` | Atendimento aberto para veículo e cliente válidos. | Iniciar diagnóstico. |
| `EmDiagnostico` | Cadastro de diagnóstico, serviços e unidades de peça. | Gerar orçamento com ao menos um item. |
| `AguardandoAprovacao` | Orçamento gerado, aguardando decisão do cliente. | Aprovar ou rejeitar orçamento pendente. |
| `EmExecucao` | Orçamento aprovado, execução iniciada e peças reservadas. | Finalizar manutenção. |
| `Finalizada` | Execução encerrada, com peças consumidas. | Entregar veículo. |
| `Entregue` | Atendimento concluído e veículo entregue. | Estado final do fluxo entregue. |

### Estados do orçamento

- `Pendente`: orçamento gerado; ainda não foi aprovado nem rejeitado.
- `Aprovado`: o cliente proprietário decidiu aprovar; a execução pode começar.
- `Rejeitado`: o cliente proprietário decidiu rejeitar; a execução não é iniciada.

**A rejeição não cria um estado `Rejeitada` na OS.** A OS permanece em `AguardandoAprovacao`, enquanto o status do `Orcamento` passa a `Rejeitado`. Como o orçamento deixa de estar pendente, o fluxo implementado não permite aprovar esse mesmo orçamento depois da rejeição.

## 5. Regras de negócio e autorização

### Identificação de quem decide o orçamento

- A oficina (`AdminOnly`) **gera** o orçamento durante o diagnóstico.
- O cliente (`ClienteOnly`) **aprova ou rejeita** o orçamento; a decisão pertence ao proprietário da OS.
- O identificador do cliente é lido da claim autenticada **`cliente_id` do JWT**, e **não** recebido como dado confiável do corpo ou da query string.
- A Application/Use Case verifica se `OrdemServico.ClienteId` corresponde ao identificador autenticado antes da alteração. Para OS ausente ou de outro proprietário, a operação não modifica o domínio.
- A aprovação só pode ocorrer com orçamento `Pendente` e peças disponíveis; a reserva faz parte da operação transacional.
- A rejeição só pode ocorrer com orçamento `Pendente` e não inicia a execução.

### Regras complementares

- Um veículo usado na abertura da OS deve pertencer ao cliente informado.
- Serviços e peças são adicionados apenas durante `EmDiagnostico`.
- A mesma unidade (identificador) de peça não pode ser incluída duas vezes na mesma OS.
- O orçamento não pode ser gerado sem itens de serviço ou peça.
- A finalização exige execução iniciada; as peças reservadas são consumidas para a mesma OS.
- A entrega exige OS `Finalizada`.

## 6. Estoque por unidade

O cadastro de `PecaInsumo` modela **cada unidade física individualmente**. 

| Status da unidade | Definição | Transição no fluxo |
| --- | --- | --- |
| `Disponivel` | A unidade pode ser usada em um orçamento. | Aprovação reserva a unidade. |
| `Reservada` | A unidade está vinculada a uma OS específica. | Finalização consome a unidade. |
| `Consumida` | A unidade foi empregada na manutenção. | Sem retorno ao estoque no fluxo entregue. |

**Observação:** adicionar uma peça à OS durante o diagnóstico **não** a reserva; a reserva ocorre quando o orçamento é aprovado.

## 7. Comandos, eventos e políticas — Event Storming

Os nomes de **eventos de domínio** abaixo representam *fatos de negócio identificados na modelagem*. 

| Comando / intenção de negócio | Fato resultante | Condição relevante |
| --- | --- | --- |
| Cadastrar Cliente | Cliente Cadastrado | Documento válido e sem duplicação. |
| Cadastrar Veículo | Veículo Cadastrado | Cliente existente e vínculo válido. |
| Abrir Ordem de Serviço | OS Criada | Veículo pertence ao cliente. |
| Iniciar Diagnóstico | Diagnóstico Iniciado | OS `Recebida`. |
| Registrar Diagnóstico | Diagnóstico Registrado | OS `EmDiagnostico`. |
| Adicionar Serviço ou Peça | Item Adicionado | OS `EmDiagnostico`; unidade de peça disponível. |
| Gerar Orçamento | Orçamento Gerado | OS em diagnóstico com itens. |
| Aprovar Orçamento (Cliente) | Orçamento Aprovado; Peças Reservadas; Execução Iniciada | Cliente proprietário autenticado, orçamento pendente e peças disponíveis. |
| Rejeitar Orçamento (Cliente) | Orçamento Rejeitado | Cliente proprietário autenticado e orçamento pendente. |
| Finalizar OS | OS Finalizada; Peças Consumidas | OS `EmExecucao`. |
| Registrar Entrega | Veículo Entregue | OS `Finalizada`. |
| Consultar Acompanhamento | Consulta sem evento de mutação | Cliente autenticado é proprietário da OS. |
| Cadastrar Peça/Insumo | Unidade Cadastrada | Cadastro individual de peça. |

**Disponibilizar orçamento ao cliente:** este é um objetivo/etapa de negócio representado no Event Storming, mas a implementação consultada **não possui um endpoint GET específico para o cliente obter itens e totais do orçamento**. Existe `GET /api/ordens-servico/{id}/detalhes` protegido por `AdminOnly`; o cliente possui `GET /api/acompanhamento/{id}` para consultar o status da própria OS, no mesmo endpoint possui informações da OS como orçamento, etc…

## 8. Agregados e objetos de valor

| Raiz de agregado | Entidades/objetos associados | Limite de consistência |
| --- | --- | --- |
| `Cliente` | `Documento` (Value Object) | Identidade e documento do cliente. |
| `Veiculo` | `Placa` (Value Object), `ClienteId` | Identificação e vínculo do veículo. |
| `Servico` | Nome, descrição, preço | Catálogo de serviços. |
| `PecaInsumo` | Status e identificação da OS reservante | Unidade de estoque individual. |
| `OrdemServico` | `Diagnostico`, `Orcamento`, `ItemServico`, `ItemPecaOS` | Ciclo da OS, orçamento, itens e transições. |

`Orcamento` **não é uma raiz de agregado independente**; sua aprovação/rejeição respeita as invariantes de `OrdemServico`.

Os objetos de valor implementados são `Documento` e `Placa`. `StatusOS`, `StatusOrcamento` e `StatusPecaInsumo` são enums; não devem ser descritos como classes Value Object.

## 9. Limites técnicos do MVP

- Os papéis de Atendente, Mecânico e Estoquista não possuem políticas JWT distintas.
- A autenticação de demonstração (`DemoAuth`) só é habilitada no ambiente `Development`, quando explicitamente configurada; não equivale a login real de produção.

## 10. Rastreabilidade do código e endpoints

| Responsabilidade | Implementação |
| --- | --- |
| Cadastrar e operacionalizar a OS, gerar orçamento | `src/Oficina.Api/Controllers/OrdensServicoController.cs` — ações `AdminOnly` |
| Aprovar e rejeitar orçamento | **O mesmo `OrdensServicoController`** — ações `ClienteOnly`, leitura da claim `cliente_id` |
| Conferência de propriedade e reserva transacional | `src/Oficina.Application/UseCases/AprovarOrcamentoUseCase.cs` |
| Rejeitar orçamento e conferir propriedade | `OrdemServicoApplicationService.RejeitarOrcamentoAsync` |
| Consultar status como cliente | `src/Oficina.Api/Controllers/AcompanhamentoController.cs` |
| Domínio e transições | `src/Oficina.Domain/Entities/OrdemServico.cs`, `Orcamento.cs`, `PecaInsumo.cs` |
| Repositórios, EF Core e transações | `src/Oficina.Infrastructure/Persistence/` |
| Migrations isoladas | `src/Oficina.DbMigrator/` |
| Configuração das políticas e JWT | `src/Oficina.Api/Program.cs` |

### Contratos HTTP relevantes

| Operação | Rota | Autorização |
| --- | --- | --- |
| Gerar orçamento | `POST /api/ordens-servico/{id}/orcamento` | `AdminOnly` |
| Aprovar orçamento | `POST /api/ordens-servico/{id}/orcamento/aprovar` | `ClienteOnly` + propriedade da OS |
| Rejeitar orçamento | `POST /api/ordens-servico/{id}/orcamento/rejeitar` | `ClienteOnly` + propriedade da OS |
| Consultar status da própria OS | `GET /api/acompanhamento/{id}` | `ClienteOnly` + propriedade da OS |
| Consultar detalhes administrativos | `GET /api/ordens-servico/{id}/detalhes` | `AdminOnly` |

## 11. Documentos relacionados

- [`event-storming.pdf`](event-storming.pdf): comandos, fatos e políticas do processo de negócio.
- [`diagramas.pdf`](diagramas.pdf): C4 níveis 1, 2 e 3, além do modelo de agregados.
- [`c4-nivel-1.mmd`](c4-nivel-1.mmd), [`c4-nivel-2.mmd`](c4-nivel-2.mmd) e [`c4-nivel-3.mmd`](c4-nivel-3.mmd)
- [Board no Miro](https://miro.com/app/board/uXjVEbdPNvM=/).
- [GitHub — branch `dev`](https://github.com/davidnunesv/oficina-mecanica/tree/dev).