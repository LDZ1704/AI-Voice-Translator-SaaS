# Hướng dẫn Setup MoMo Payment Gateway

## 1. Đăng ký tài khoản MoMo Business (M4B)

**Lưu ý quan trọng**: Để tích hợp MoMo Payment Gateway, bạn cần có **tài khoản MoMo Business (M4B)**, không phải developer account.

### 1.1. Đăng ký MoMo Business

1. **Truy cập MoMo Business Portal**
   - Website: https://business.momo.vn/
   - Hoặc liên hệ hotline: 1900 545426

2. **Đăng ký tài khoản**
   - Điền thông tin doanh nghiệp/cá nhân
   - Xác thực tài khoản theo hướng dẫn
   - Kích hoạt dịch vụ thanh toán

3. **Sau khi đăng ký thành công**, bạn sẽ nhận được:
   - **Partner Code**: Mã đối tác (ví dụ: `MOMO` hoặc mã riêng của bạn)
   - **Access Key**: Khóa truy cập API
   - **Secret Key**: Khóa bí mật (quan trọng, giữ bí mật)

### 1.2. Nếu không thể đăng ký trực tiếp

Nếu bạn gặp khó khăn trong việc đăng ký:

1. **Liên hệ trực tiếp MoMo Support**:
   - Email: support@momo.vn
   - Hotline: 1900 545426
   - Website hỗ trợ: https://developers.momo.vn/
   - Yêu cầu hỗ trợ đăng ký tài khoản Business và tích hợp API

2. **Đăng ký qua đối tác**:
   - Một số platform như Sapo, Haravan có hỗ trợ tích hợp MoMo
   - Có thể đăng ký qua các đối tác này nếu phù hợp

3. **Sử dụng Sandbox/Test Environment** (cho development):
   - Liên hệ MoMo để xin test credentials
   - Hoặc sử dụng test credentials từ đối tác (nếu có)

### 1.3. Thông tin cần chuẩn bị khi đăng ký

- Thông tin doanh nghiệp/cá nhân
- Giấy phép kinh doanh (nếu là doanh nghiệp)
- CMND/CCCD (nếu là cá nhân)
- Thông tin tài khoản ngân hàng để nhận tiền
- Website/app cần tích hợp thanh toán

## 3. Cấu hình trong appsettings.json

Sau khi có đầy đủ thông tin từ MoMo Business, cập nhật file `src/appsettings.json`:

Thêm vào file `src/appsettings.json`:

```json
{
  "MoMoPayment": {
    "MomoApiUrl": "https://test-payment.momo.vn/gw_payment/transactionProcessor",
    "SecretKey": "YOUR_SECRET_KEY_HERE",
    "AccessKey": "YOUR_ACCESS_KEY_HERE",
    "ReturnUrl": "http://localhost:5172/Billing/PaymentReturn",
    "NotifyUrl": "http://localhost:5172/Billing/PaymentNotify",
    "PartnerCode": "MOMO",
    "RequestType": "captureMoMoWallet"
  }
}
```

### Giải thích các tham số:

- **MomoApiUrl**: Endpoint API của MoMo (theo Google Docs)
  - Sandbox: `https://test-payment.momo.vn/gw_payment/transactionProcessor`
  - Production: `https://payment.momo.vn/gw_payment/transactionProcessor`
  - Hoặc có thể dùng endpoint v2: `https://test-payment.momo.vn/v2/gateway/api/create`
- **SecretKey**: Secret key từ dashboard MoMo (dùng để tạo chữ ký HMAC-SHA256)
- **AccessKey**: Access key từ dashboard MoMo
- **ReturnUrl**: URL redirect sau khi user thanh toán xong (có thể override trong code)
- **NotifyUrl**: URL nhận IPN callback từ MoMo (có thể override trong code)
- **PartnerCode**: Mã đối tác từ MoMo (thường là `MOMO` cho sandbox)
- **RequestType**: Loại request thanh toán
  - `captureMoMoWallet`: Thanh toán qua ví MoMo (khuyến nghị, theo Google Docs)
  - `captureWallet`: Tương tự captureMoMoWallet
  - `payWithMethod`: Thanh toán qua nhiều phương thức (CollectionLink)

## 4. Cấu hình Callback URLs

Trong MoMo dashboard, cấu hình các URL callback:

### Return URL (Redirect URL):
```
https://your-domain.com/Billing/PaymentReturn
```
URL này sẽ được gọi khi user hoàn tất thanh toán trên MoMo và quay lại website.

### IPN URL (Instant Payment Notification):
```
https://your-domain.com/Billing/PaymentNotify
```
URL này sẽ được MoMo gọi để notify về kết quả thanh toán (async).

**Lưu ý**: 
- Các URL phải là HTTPS trong production
- Có thể dùng ngrok hoặc localtunnel để test local: `https://your-ngrok-url.ngrok.io/Billing/PaymentReturn`

## 5. Test với MoMo Sandbox

### Số điện thoại test:
- Số điện thoại: `0123456789` (hoặc số bất kỳ)
- OTP: `000000` (6 số 0)

### Các trường hợp test:
1. **Thanh toán thành công**: Nhập số điện thoại và OTP `000000`
2. **Thanh toán thất bại**: Nhập sai OTP hoặc hủy thanh toán

## 6. Chuyển sang Production

Khi sẵn sàng chuyển sang production:

1. Đổi `IsSandbox` thành `false`
2. Đổi `Endpoint` thành production endpoint
3. Cập nhật `PartnerCode`, `AccessKey`, `SecretKey` với thông tin production
4. Đảm bảo callback URLs là HTTPS và accessible từ internet
5. Test kỹ với số tiền nhỏ trước

## 7. Xử lý lỗi thường gặp

### Lỗi "Invalid signature":
- Kiểm tra lại `SecretKey` có đúng không
- Đảm bảo thứ tự các tham số trong rawHash đúng theo MoMo docs

### Lỗi "Partner not found":
- Kiểm tra `PartnerCode` có đúng không
- Đảm bảo ứng dụng đã được kích hoạt trong MoMo dashboard

### Callback không được gọi:
- Kiểm tra URL có accessible từ internet không (dùng ngrok cho local)
- Kiểm tra firewall/security settings
- Xem logs trong MoMo dashboard

## 8. Code Example và GitHub Repository

### GitHub Repository chính thức:
- **MoMo Payment Examples**: https://github.com/momo-wallet/payment
- Repository này chứa code examples cho nhiều ngôn ngữ: C#, PHP, Python, NodeJS, Ruby, Go
- Code example C#: https://github.com/momo-wallet/payment/tree/master/c%23

### Các điểm quan trọng từ code example:

1. **Signature Encoding**: 
   - Sử dụng `ASCIIEncoding` cho signature (không phải UTF8)
   - Code: `Encoding.ASCII.GetBytes(text)` và `Encoding.ASCII.GetBytes(key)`

2. **Request Type**:
   - `captureWallet`: Thanh toán qua ví MoMo (phù hợp cho subscription)
   - `payWithMethod`: Thanh toán qua nhiều phương thức (CollectionLink)

3. **Thứ tự các field trong signature**:
   ```
   accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}
   ```

4. **IPN Callback**:
   - MoMo sẽ gọi IPN URL sau khi thanh toán
   - Cần verify signature từ callback data
   - Luôn trả về HTTP 200 OK để MoMo biết đã nhận được

## 9. Tài liệu tham khảo và Liên hệ

### Tài liệu chính thức:
- **MoMo Business Portal**: https://business.momo.vn/
- **MoMo Developers**: https://developers.momo.vn/
- **API Documentation**: https://developers.momo.vn/docs/ (cần đăng nhập)
- **GitHub Examples**: https://github.com/momo-wallet/payment

### Liên hệ hỗ trợ:
- **Email**: support@momo.vn
- **Hotline**: 1900 545426
- **Thời gian hỗ trợ**: Thứ 2 - Thứ 6, 8:00 - 17:00

### Các nguồn tham khảo khác:
- Hướng dẫn tích hợp MoMo trên Sapo: https://help.sapo.vn/tich-hop-thanh-toan-momo-tren-website-sapo
- Các đối tác tích hợp MoMo: Liên hệ MoMo để biết danh sách đối tác chính thức

## 10. Phương án thay thế khi chưa có MoMo Business Account

Nếu bạn chưa thể đăng ký MoMo Business ngay, có thể sử dụng các phương án sau:

### Option 1: Tạm thời bỏ qua thanh toán MoMo
- Comment code liên quan đến MoMo trong `BillingController`
- Sử dụng flow thanh toán giả lập (mock payment) để test các tính năng khác
- Khi có MoMo credentials, uncomment và cấu hình lại

### Option 2: Sử dụng cổng thanh toán khác
- **VNPay**: Phổ biến tại Việt Nam, dễ đăng ký hơn
- **Stripe**: Quốc tế, hỗ trợ thẻ tín dụng
- **PayPal**: Quốc tế, phổ biến
- Tích hợp tương tự như MoMo

### Option 3: Liên hệ trực tiếp MoMo
- Gửi email đến support@momo.vn với nội dung:
  - Giới thiệu về dự án
  - Mục đích sử dụng MoMo Payment
  - Yêu cầu hỗ trợ đăng ký và test credentials
- Thường sẽ được hỗ trợ trong 1-3 ngày làm việc

## 11. Lưu ý bảo mật

- **KHÔNG** commit `SecretKey` vào Git
- Sử dụng User Secrets hoặc Azure Key Vault cho production
- Luôn validate signature từ MoMo callback
- Log tất cả payment transactions để audit

## 12. Ví dụ cấu hình User Secrets (cho development)

```bash
dotnet user-secrets set "MoMoPayment:SecretKey" "YOUR_SECRET_KEY"
dotnet user-secrets set "MoMoPayment:AccessKey" "YOUR_ACCESS_KEY"
```

Sau đó trong `appsettings.json` chỉ cần:
```json
{
  "MoMoPayment": {
    "PartnerCode": "MOMO",
    "Endpoint": "https://test-payment.momo.vn/v2/gateway/api/create",
    "IsSandbox": true
  }
}
```

