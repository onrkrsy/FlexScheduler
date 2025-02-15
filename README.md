# FlexScheduler

FlexScheduler, .NET 8 ve Hangfire ile oluşturulmuş esnek bir HTTP iş zamanlama hizmetidir. HTTP işlerini zamanlamak ve yönetmek için basit bir yol sağlar ve hem yinelenen hem de gecikmeli yürütmeyi destekler.

## Özellikler

- Cron ifadeleri kullanarak yinelenen HTTP işleri zamanlayın
- Özel gecikme aralıkları ile gecikmeli HTTP işleri zamanlayın
- Bearer tokenlar ile iş kimlik doğrulaması desteği
- Yapılandırılabilir HTTP zaman aşımı ve yeniden deneme politikaları
- Önceden tanımlanmış işler için JSON yapılandırması
- Kuyruk önceliklendirmesi ile birden fazla sunucu desteği
- Temel kimlik doğrulama ile güvenli Hangfire kontrol paneli
- İş yönetimi için RESTful API

## Gereksinimler

- .NET 8.0 SDK
- SQL Server (LocalDB veya tam sürüm)
- Visual Studio 2022 veya VS Code

## Hızlı Başlangıç

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

### Bağlantı Dizgisi

`appsettings.json` dosyasını SQL Server bağlantı dizginizle güncelleyin:

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

Yinelenen HTTP işlerinizi `Configurations/httpJobs.json` dosyasında tanımlayın. İşte bazı örnekler:

```json
{
  "Jobs": [
    {
      "JobId": "WeatherForecast-HealthCheck",
      "CronExpression": "*/5 * * * *",
      "Url": "http://localhost:5000/WeatherForecast",
      "HttpMethod": "GET",
      "RequiresAuthentication": false,
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
      "RequiresAuthentication": true,
      "TimeoutInSeconds": 300,
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

İşleriniz kimlik doğrulaması gerektiriyorsa, giriş ayarlarını yapılandırın:

```json
{
  "LoginSettings": {
    "UserName": "your-username",
    "Password": "your-password",
    "Application": "FlexScheduler",
    "SystemUserType": "System",
    "LoginEndpoint": "http://your-auth-server/api/auth/login"
  }
}
```

## API Uç Noktaları

### Yinelenen İş Oluşturma
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

### Gecikmeli İş Oluşturma
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

### İş Silme
```http
DELETE /api/jobs/{jobId}
```

### İş Varlığını Kontrol Etme
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

3. **Ağ Güvenliği**:
   - Tüm uç noktalar için HTTPS kullanın
   - Uygun ağ segmentasyonu uygulayın
   - Uygun zaman aşımı ayarlarını yapılandırın

## İzleme

1. Hangfire kontrol paneline `/hangfire` adresinden erişin
2. İş yürütme durumu ve geçmişini izleyin
3. Gerçek zamanlı istatistikler ve sunucu sağlığını görüntüleyin
4. Başarısız işleri kontrol edin ve gerekirse yeniden deneyin

## Sorun Giderme

1. **İş Hataları**:
   - Hangfire kontrol panelinde iş detaylarını kontrol edin
   - Hata mesajları için uygulama günlüklerini inceleyin
   - Uç nokta kullanılabilirliğini ve kimlik doğrulamasını doğrulayın

2. **Kontrol Paneli Erişim Sorunları**:
   - `HangfireSettings`'teki kimlik bilgilerini doğrulayın
   - Ağ bağlantısını kontrol edin
   - Kimlik doğrulama hataları için sunucu günlüklerini inceleyin

3. **Performans Sorunları**:
   - İşçi sayısını ve kuyruk uzunluklarını izleyin
   - Gerekirse sunucu yapılandırmasını ayarlayın
   - Yoğun kuyruklar için daha fazla işçi eklemeyi düşünün

## Lisans

Bu proje MIT Lisansı ile lisanslanmıştır.