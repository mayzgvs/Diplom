using Service.Data;
using Service.Services;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Service.Services
{
    public class NotificationService
    {
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;

        private const int READY_STATUS_ID = 3;

        public NotificationService()
        {
            _emailService = new EmailService();
            _smsService = new SmsService();
        }

        public async Task SendNotificationOnStatusChange(RepairRequest request, int oldStatusId, int newStatusId)
        {
            if (newStatusId == READY_STATUS_ID && oldStatusId != READY_STATUS_ID)
            {
                await SendReadyNotification(request);
            }
        }

        public async Task SendManualNotificationAsync(RepairRequest request, bool sendEmail, bool sendSms)
        {
            var client = request.Car?.Client;
            if (client == null) return;

            var carInfo = $"{request.Car.Brand} {request.Car.Model} ({request.Car.RegistrationNumber})";

            decimal totalCost = request.TotalCost; 

            var orderData = new
            {
                OrderNumber = request.Id.ToString("D9"),
                OrderDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                Organization = "ОБЩЕСТВО С ОГРАНИЧЕННОЙ ОТВЕТСТВЕННОСТЬЮ \"АВТОСЕРВИС\"",
                ClientName = client.FullName ?? "-",
                ClientContact = client.ContactNumber ?? "-",
                DocumentNumber = "000000001",
                DocumentDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                Warehouse = "Основной склад автосервиса",
                PlannedDate = DateTime.Now.ToString("dd.MM.yyyy"),
                PaymentType = "Наличные",
                PaymentDate = DateTime.Now.ToString("dd.MM.yyyy"),
                DelayDays = "0",
                PaymentStatus = "Не оплачен",
                DeliveryMethod = "Самовывоз",
                DeliveryAddress = "-", 
                TransportCompany = "ИП Автосервис",
                Responsible = "Менеджер автосервиса",
                Items = new[]
                {
                    new
                    {
                        Name = $"Ремонт автомобиля {carInfo}",
                        Quantity = 1,
                        Unit = "шт",
                        Price = totalCost.ToString("F2"),
                        VatRate = "Без НДС",
                        SumWithoutVat = totalCost.ToString("F2"),
                        SumVat = "0",
                        Total = totalCost.ToString("F2")
                    }
                },
                TotalWithoutVat = totalCost.ToString("F2"),
                TotalVat = "0",
                GrandTotal = totalCost.ToString("F2")
            };

            string smsMessage = $"Уважаемый(ая) {client.FullName}! Ваш автомобиль {carInfo} готов к выдаче. Ждем Вас в автосервисе.";
            bool emailSent = false;
            bool smsSent = false;

            if (sendEmail && !string.IsNullOrWhiteSpace(client.Email))
            {
                var subject = $"Ваш заказ № {orderData.OrderNumber} успешно сформирован";
                var htmlBody = GenerateHtmlEmail(orderData);
                emailSent = await _emailService.SendEmailAsync(client.Email, subject, htmlBody);
            }

            if (sendSms && !string.IsNullOrWhiteSpace(client.ContactNumber))
            {
                smsSent = await _smsService.SendSmsAsync(client.ContactNumber, smsMessage);
            }

            if (emailSent || smsSent)
            {
                MessageBox.Show($"Уведомления отправлены:\n" +
                               $"{(emailSent ? "✓ Email\n" : "")}" +
                               $"{(smsSent ? "✓ SMS" : "")}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (sendEmail || sendSms)
            {
                MessageBox.Show("Не удалось отправить уведомления. Проверьте настройки.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task SendReadyNotification(RepairRequest request)
        {
            await SendManualNotificationAsync(request, true, true);
        }

        private string GenerateHtmlEmail(dynamic data)
        {
            var itemsHtml = new StringBuilder();
            int counter = 1;

            foreach (var item in data.Items)
            {
                itemsHtml.AppendLine($@"
                    <tr style='border-bottom: 1px solid #e0e0e0;'>
                        <td style='padding: 10px; text-align: center;'>{counter++}</td>
                        <td style='padding: 10px;'>{item.Name}</td>
                        <td style='padding: 10px; text-align: center;'>{item.Quantity}</td>
                        <td style='padding: 10px; text-align: center;'>{item.Unit}</td>
                        <td style='padding: 10px; text-align: right;'>{item.Price}</td>
                        <td style='padding: 10px; text-align: center;'>{item.VatRate}</td>
                        <td style='padding: 10px; text-align: right;'>{item.SumWithoutVat}</td>
                        <td style='padding: 10px; text-align: right;'>{item.SumVat}</td>
                        <td style='padding: 10px; text-align: right;'>{item.Total}</td>
                    </td>");
            }

            var paymentStatusClass = data.PaymentStatus == "Оплачен" ? "badge-paid" : "badge-not-paid";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Заказ № {data.OrderNumber}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            max-width: 1100px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #3498DB 0%, #2980B9 100%);
            color: white;
            padding: 25px 30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .header p {{
            margin: 8px 0 0;
            opacity: 0.9;
            font-size: 14px;
        }}
        .content {{
            padding: 30px;
        }}
        .greeting {{
            margin-bottom: 25px;
            color: #2c3e50;
            line-height: 1.6;
        }}
        .greeting strong {{
            font-size: 16px;
        }}
        .info-block {{
            margin-bottom: 30px;
        }}
        .info-title {{
            font-weight: bold;
            font-size: 16px;
            color: #3498DB;
            border-left: 4px solid #3498DB;
            padding-left: 12px;
            margin-bottom: 15px;
        }}
        .info-table {{
            width: 100%;
            border-collapse: collapse;
            background: #f8f9fa;
            border-radius: 6px;
            overflow: hidden;
            font-size: 14px;
        }}
        .info-table td {{
            padding: 10px 15px;
            border-bottom: 1px solid #e0e0e0;
        }}
        .info-table td:first-child {{
            font-weight: bold;
            width: 35%;
            background: #f0f2f5;
        }}
        .info-table tr:last-child td {{
            border-bottom: none;
        }}
        .badge-paid {{
            color: #27ae60;
            font-weight: bold;
        }}
        .badge-not-paid {{
            color: #e74c3c;
            font-weight: bold;
        }}
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
            font-size: 13px;
        }}
        .items-table th {{
            background: #3498DB;
            color: white;
            padding: 12px 8px;
            text-align: center;
            font-weight: 500;
            font-size: 13px;
        }}
        .items-table td {{
            padding: 10px 8px;
            border-bottom: 1px solid #e0e0e0;
        }}
        .items-table tr:hover {{
            background-color: #f5f5f5;
        }}
        .totals {{
            text-align: right;
            margin-top: 20px;
            padding-top: 15px;
            border-top: 2px solid #e0e0e0;
        }}
        .totals p {{
            margin: 5px 0;
            font-size: 14px;
        }}
        .totals .grand-total {{
            font-size: 18px;
            font-weight: bold;
            color: #3498DB;
            margin-top: 10px;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #7f8c8d;
            font-size: 12px;
            border-top: 1px solid #e0e0e0;
        }}
        @media (max-width: 768px) {{
            .content {{
                padding: 15px;
            }}
            .items-table {{
                font-size: 11px;
            }}
            .items-table th,
            .items-table td {{
                padding: 6px 4px;
            }}
            .info-table td {{
                padding: 8px 10px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Ваш заказ № {data.OrderNumber} успешно сформирован</h1>
            <p>Информация по документу реализации товаров</p>
        </div>
        
        <div class='content'>
            <div class='greeting'>
                <strong>Уважаемые представители компании {data.ClientName}!</strong><br/><br/>
                Сообщаем Вам, что заказ по документу реализации успешно сформирован и проведен в системе.
            </div>

            <div class='info-block'>
                <div class='info-title'>Реквизиты заказа</div>
                <table class='info-table'>
                    <tr><td>Номер документа</td><td>{data.OrderNumber}</td></tr>
                    <tr><td>Дата документа</td><td>{data.OrderDate}</td></tr>
                    <tr><td>Организация</td><td>{data.Organization}</td></tr>
                    <tr><td>Контрагент</td><td>{data.ClientName}</td></tr>
                    <tr><td>Документ контрагента</td><td>{data.DocumentNumber} от {data.DocumentDate}</td></tr>
                    <tr><td>Склад отгрузки</td><td>{data.Warehouse}</td></tr>
                    <tr><td>Плановая дата отгрузки</td><td>{data.PlannedDate}</td></tr>
                    <tr><td>Тип оплаты</td><td>{data.PaymentType}</td></tr>
                    <tr><td>Дата оплаты</td><td>{data.PaymentDate}</td></tr>
                    <tr><td>Отсрочка платежа, дней</td><td>{data.DelayDays}</td></tr>
                    <tr><td>Статус оплаты</td><td class='{paymentStatusClass}'>{data.PaymentStatus}</td></tr>
                    <tr><td>Способ доставки</td><td>{data.DeliveryMethod}</td></tr>
                    <tr><td>Адрес доставки</td><td>{data.DeliveryAddress}</td></tr>
                    <tr><td>Транспортная компания</td><td>{data.TransportCompany}</td></tr>
                    <tr><td>Ответственный</td><td>{data.Responsible}</td></tr>
                </table>
            </div>

            <div class='info-block'>
                <div class='info-title'>Состав заказа</div>
                <table class='items-table'>
                    <thead>
                        <tr>
                            <th>№</th>
                            <th>Номенклатура</th>
                            <th>Кол-во</th>
                            <th>Ед.</th>
                            <th>Цена</th>
                            <th>Ставка НДС</th>
                            <th>Сумма без НДС</th>
                            <th>Сумма НДС</th>
                            <th>Сумма</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsHtml}
                    </tbody>
                </table>
            </div>

            <div class='totals'>
                <p><strong>Итого без НДС:</strong> {data.TotalWithoutVat}</p>
                <p><strong>Итого НДС:</strong> {data.TotalVat}</p>
                <p class='grand-total'><strong>Общая сумма:</strong> {data.GrandTotal}</p>
            </div>

            <div class='greeting'>
                Благодарим Вас за сотрудничество.<br/>
                При возникновении вопросов Вы можете связаться с нами ответным письмом.
            </div>
        </div>
        
        <div class='footer'>
            С уважением,<br/>
            Команда автосервиса
        </div>
    </div>
</body>
</html>";
        }
    }
}