# 🔐 CipherTool - Secure File Encryption

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white)
![MIT License](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)

**CipherTool**, dosyalarınızı askeri düzeyde şifreleme standartları ile koruma altına alan, modern ve hızlı bir masaüstü uygulamasıdır. 

---

## 📸 Uygulama Görünümü

![Uygulama Ekran Görüntüsü](CipherTool.png)



---

## ✨ Özellikler

- **🛡️ Maksimum Güvenlik:** AES-256-CBC algoritması ile tam koruma.
- **🔑 Akıllı Anahtar Türetme:** PBKDF2 ve 200.000+ iterasyon ile brute-force saldırılarına karşı dirençli.
- **🎨 Modern UI:** Cursor/VS Code estetiğinde karanlık tema (Dark Mode).
- **🖱️ Sürükle-Bırak:** Dosyalarınızı hızla şifrelemek için sürüklemeniz yeterli.
- **⚡ Yüksek Performans:** Optimize edilmiş asenkron dosya okuma/yazma işlemleri.

---

## 🛠️ Nasıl Çalışır?

1. **Şifreleme:** Dosyanız için rastgele bir **Salt** ve **IV** üretilir. Parolanız bu salt ile birleşerek anahtara dönüşür.
2. **Paketleme:** Şifrelenen veri, `.cipher` uzantısı ile kaydedilir.
3. **Çözme:** Sadece doğru parola ve dosya içindeki gizli salt verisi eşleştiğinde dosya orijinal haline döner.

## 🚀 Kurulum

1. Depoyu klonlayın:
   ```bash
   git clone [https://github.com/kullaniciadi/CipherTool.git](https://github.com/kullaniciadi/CipherTool.git)