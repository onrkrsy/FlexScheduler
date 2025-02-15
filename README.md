# FlexScheduler

**FlexScheduler**, .NET 8 ve Hangfire kullanılarak geliştirilmiş esnek bir Job Scheduling servisidir. Arka planda çalışacak işlerin (Background Jobs) zamanlanmasını ve yönetilmesini kolaylaştıran bir altyapı sunar.
 
Özellikle farklı dillerde veya frameworklerde yazılmış servislerin bulunduğu yapılarda, merkezi bir job scheduling servisine duyulan ihtiyaç duyularbilir. FlexScheduler, **Hangfire**’ın sağladığı esnekliği kullanarak tüm job'ları tek bir noktadan yönetmeyi mümkün kılar.
FlexScheduler, **HTTP tabanlı** job yönetimi sağlar. Bu sayede, arka planda çalışacak işler ilgili mikroservislerde geliştirilir ve FlexScheduler yalnızca bu servislerin belirlenen zamanlarda çağrılmasını sağlar. Yani, job'lar aslında zamanlanmış HTTP istekleri olarak çalışır.
Ayrıca, **özel (custom) job**'lar için de altyapı hazırdır. Yani, farklı iş gereksinimleri için esnek bir job çalıştırma mekanizması sunmaktadır.

## Özellikler

- Appsettings ile **Kuyruk** önceliklendirmesi ve **birden fazla sunucu** desteği
- Cron ifadeleri kullanarak **yinelenen** HTTP işleri zamanlama
- Özel gecikme aralıkları ile **gecikmeli** HTTP işleri zamanlama
- Bearer tokenlar ile identity service doğrulaması desteği (microservis mimarilerinde bulunan merkezi auth servisleri için)
- Yapılandırılabilir HTTP zaman aşımı ve yeniden deneme politikaları
- Önceden tanımlanmış HTTP işler için **JSON** yapılandırması- 
- Basic authentication ile Hangfire kontrol paneli
- Yeni Http Job yönetimi için endpointler


## Başlangıç

1. Depoyu klonlayın
2. `appsettings.json` dosyasındaki bağlantı dizgilerini ve ayarları güncelleyin
3. Aşağıdaki komutları çalıştırın:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```
4. Hangfire kontrol paneline `/hangfire` adresinden erişin (varsayılan kimlik bilgileri: admin/admin)

## Yapılandırma

### Bağlantı Ayarı

`appsettings.json` dosyasını SQL Server bağlantı dizginizle güncelleyin: (_connection stringdeki database oluşturulmuş olmalı_)

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Server=(localdb)\\mssqllocaldb;Database=FlexScheduler;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Hangfire Ayarları

Hangfire kontrol paneli kimlik doğrulaması ve sunucu ayarlarını yapılandırın:

```json
{
  "HangfireSettings": {
    "UserName": "admin",
    "Password": "your-secure-password",
    "ServerList": [
      {
        "Name": "default-server",
        "WorkerCount": 5,
        "QueueList": [
          "critical",
          "default",
          "long-running"
        ]
      },
      {
        "Name": "background-server",
        "WorkerCount": 2,
        "QueueList": [
          "background",
          "low-priority"
        ]
      }
    ]
  }
}
```

### HTTP İşleri Yapılandırması

Yinelenen HTTP işlerinizi `Configurations/httpJobs.json` dosyasında tanımlayın. 
- **TimeoutInSeconds**: Yapılan request'in timeout süresi 
- **RequiresAuthentication**: Merkezi bir identity service'den authentication gerekli mi?
- **IsEnabled**: Proje ayağa kalkarken job'ın eklenip eklenmeyeceği ayarı.
- **Headers**: İhtiyaç haline header'a key value olarak istenilen değerler eklenebilir.
- **Tags ve Descripton**: Sadece json dosyasında joblar için bilgilendirme alanlarıdır. Businenss içerisinde kullanılmamaktadır.
İşte bazı örnekler:

```json
{
  "Jobs": [
    {
      "JobId": "WeatherForecast-HealthCheck",
      "CronExpression": "*/5 * * * *",
      "Url": "http://localhost:5000/WeatherForecast",
      "HttpMethod": "GET",
      "RequiresAuthentication": true,
      "TimeoutInSeconds": 30,
      "IsEnabled": true,
      "Tags": [ "weather-service", "monitoring" ],
      "Description": "Weather Forecast API sağlık kontrolü - her 5 dakikada bir"
    },
    {
      "JobId": "TodoItems-Cleanup",
      "CronExpression": "0 0 * * *",
      "Url": "http://localhost:5001/api/TodoItems/cleanup",
      "HttpMethod": "POST",
      "RequiresAuthentication": false,
      "TimeoutInSeconds": 300,
      "Headers": {
           "Authorization": "Bearer token",
           "Accept": "application/json"
      }
      "Payload": {
        "olderThanDays": 30,
        "status": "completed"
      },
      "IsEnabled": true,
      "Tags": [ "todo-service", "maintenance" ],
      "Description": "30 günden eski tamamlanmış yapılacakları temizle - her gece yarısı çalışır"
    }
  ]
}
```

### Kimlik Doğrulama Ayarları

Eklenen job'da merkezi bir kimlik doğrulaması gerektiriyorsa, Identity Service ayarları:

```json
{
  "LoginSettings": {
    "ClientId": "service-name",
    "ClientSecret": "secret",
    "LoginEndpoint": "http://your-auth-server/api/auth/login"
  }
}
```

## API Uç Noktaları

### Yinelenen Job Oluşturma
```http
POST /api/jobs/recurring
Content-Type: application/json

{
  "jobId": "my-job",
  "cronExpression": "*/15 * * * *",
  "url": "http://api.example.com/endpoint",
  "httpMethod": "POST",
  "requiresAuthentication": true,
  "timeoutInSeconds": 60,
  "queue": "default",
  "payload": { "key": "value" }
}
```

### Gecikmeli Job Oluşturma
```http
POST /api/jobs/delayed
Content-Type: application/json

{
  "jobId": "my-delayed-job",
  "url": "http://api.example.com/endpoint",
  "httpMethod": "POST",
  "delay": "00:05:00",
  "requiresAuthentication": true,
  "timeoutInSeconds": 60,
  "queue": "background",
  "payload": { "key": "value" }
}
```

### Job Silme
```http
DELETE /api/jobs/{jobId}
```

### Job Kontrol Etme
```http
GET /api/jobs/{jobId}/exists
```

## Kuyruk Önceliklendirme

İşler, önceliklerine göre farklı kuyruklara atanabilir:
- `critical`: Hemen işlenmesi gereken yüksek öncelikli işler
- `default`: Normal öncelikli standart işler
- `long-running`: Tamamlanması daha uzun süren işler
- `background`: Düşük öncelikli arka plan görevleri
- `low-priority`: Bekleyebilecek en düşük öncelikli görevler

Sunucu işçilerini belirli kuyrukları işlemek üzere yapılandırmak için `HangfireSettings.ServerList`'i yapılandırın.

## Güvenlik Hususları

1. **Kontrol Paneli Güvenliği**:
   - Üretimde varsayılan kontrol paneli kimlik bilgilerini değiştirin
   - Kontrol paneli için güçlü bir şifre kullanın
   - IP kısıtlamaları uygulamayı düşünün

2. **İş Kimlik Doğrulaması**:
   - Hassas kimlik bilgilerini güvenli yapılandırma depolamasında saklayın
   - Ortam spesifik ayar dosyaları kullanın
   - Azure Key Vault veya benzeri hizmetleri kullanmayı düşünün


## İzleme

1. Hangfire kontrol paneline `/hangfire` adresinden erişin
2. İş yürütme durumu ve geçmişini izleyin
3. Gerçek zamanlı istatistikler ve sunucu sağlığını görüntüleyin
4. Başarısız işleri kontrol edin ve gerekirse yeniden deneyin




