using Amazon;
using Amazon.Auth.AccessControlPolicy;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudInvest.Core.Entities;
using CloudInvest.Infrastructure.Data;
using CloudInvest.Infrastructure.Messaging.Requests;
using Polly;
using System.Text.Json;

namespace CloudInvest.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAmazonSQS _sqsClient;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly string _queueUrl;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _sqsClient = new AmazonSQSClient(RegionEndpoint.USEast1); 
            _queueUrl = configuration["AWS:SqsQueueUrl"];

            if (string.IsNullOrEmpty(_queueUrl) || string.IsNullOrEmpty(_queueUrl))
            {
                throw new InvalidOperationException("As configurações do SQS não foram encontradas.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker iniciado em: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 5
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                if (response.Messages.Any())
                {
                    foreach (var message in response.Messages)
                    {
                        _logger.LogInformation("Mensagem recebida: {messageBody}", message.Body);

                        try
                        {
                            // 1. Processar a mensagem
                            var ativoJson = JsonSerializer.Deserialize<AtivoRequest>(message.Body);
                            _logger.LogInformation($"Mensagem recebida: {ativoJson}");


                            // Lógica de "análise" simples
                            var recomendacao = ativoJson.Preco > 25 ? "Vender" : "Comprar";

                            // 2. Salvar no banco
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                var analysis = new AnaliseAtivo
                                {
                                    Ticker = ativoJson.Ticker,
                                    PrecoAnalisado = ativoJson.Preco,
                                    Recomendacao = recomendacao,
                                    DataAnalise = DateTime.UtcNow
                                };
                                dbContext.AnalisesAtivo.Add(analysis);
                                await dbContext.SaveChangesAsync(stoppingToken);
                                _logger.LogInformation("Análise para {ticker} salva no banco.", ativoJson.Ticker);
                            }

                            // 3. Deletar a mensagem da fila
                            var deleteRequest = new DeleteMessageRequest
                            {
                                QueueUrl = _queueUrl,
                                ReceiptHandle = message.ReceiptHandle
                            };
                            await _sqsClient.DeleteMessageAsync(deleteRequest, stoppingToken);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Operação cancelada. Encerrando o worker.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao processar mensagem.");
                        }
                    }
                }
            }
        }
    }
}
