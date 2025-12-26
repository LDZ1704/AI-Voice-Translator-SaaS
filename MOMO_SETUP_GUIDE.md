# Hướng dẫn Setup MoMo Payment Gateway

## 1. Đăng ký MoMo Business (M4B)

**Lưu ý**: Cần tài khoản **MoMo Business (M4B)**, không phải developer account.

### Đăng ký
1. Truy cập: https://business.momo.vn/ hoặc hotline: 1900 545426
2. Điền thông tin doanh nghiệp/cá nhân và xác thực
3. Sau khi đăng ký thành công, nhận được:
   - **Partner Code**: Mã đối tác (ví dụ: `MOMO`)
   - **Access Key**: Khóa truy cập API
   - **Secret Key**: Khóa bí mật (giữ bí mật)

### Nếu không thể đăng ký trực tiếp
- Liên hệ MoMo Support: support@momo.vn, 1900 545426
- Hoặc đăng ký qua đối tác (Sapo, Haravan)

## 2. Cấu hình appsettings.json

Cập nhật `src/appsettings.json`:

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

### Giải thích
- **MomoApiUrl**: 
  - Sandbox: `https://test-payment.momo.vn/gw_payment/transactionProcessor`
  - Production: `https://payment.momo.vn/gw_payment/transactionProcessor`
- **SecretKey**: Dùng để tạo chữ ký HMAC-SHA256
- **AccessKey**: Access key từ dashboard
- **ReturnUrl**: URL redirect sau khi thanh toán
- **NotifyUrl**: URL nhận IPN callback từ MoMo
- **PartnerCode**: Mã đối tác (thường `MOMO` cho sandbox)
- **RequestType**: `captureMoMoWallet` (khuyến nghị)

## 3. Callback URLs

### Return URL
```
https://your-domain.com/Billing/PaymentReturn
```
URL redirect khi user hoàn tất thanh toán.

### IPN URL
```
https://your-domain.com/Billing/PaymentNotify
```
URL nhận notification từ MoMo (async).

**Lưu ý**: 
- Production phải dùng HTTPS
- Test local: dùng ngrok hoặc localtunnel

## 4. Test với Sandbox

### Thông tin test
- Số điện thoại: `0123456789` (hoặc số bất kỳ)
- OTP: `000000` (6 số 0)

### Test cases
1. Thanh toán thành công: Nhập số điện thoại và OTP `000000`
2. Thanh toán thất bại: Nhập sai OTP hoặc hủy

## 5. Chuyển sang Production

1. Đổi `MomoApiUrl` thành production endpoint
2. Cập nhật `PartnerCode`, `AccessKey`, `SecretKey` với thông tin production
3. Đảm bảo callback URLs là HTTPS và accessible
4. Test kỹ với số tiền nhỏ trước

## 6. Lỗi thường gặp

### "Invalid signature"
- Kiểm tra `SecretKey`
- Đảm bảo thứ tự các tham số trong rawHash đúng theo MoMo docs
- Sử dụng UTF-8 encoding cho signature

### "Partner not found"
- Kiểm tra `PartnerCode`
- Đảm bảo ứng dụng đã được kích hoạt trong MoMo dashboard

### Callback không được gọi
- Kiểm tra URL có accessible từ internet không
- Kiểm tra firewall/security settings
- Xem logs trong MoMo dashboard

## 7. Bảo mật

- **KHÔNG** commit `SecretKey` vào Git
- Sử dụng User Secrets hoặc Azure Key Vault cho production
- Luôn validate signature từ MoMo callback
- Log tất cả payment transactions để audit

## 8. User Secrets (Development)

```bash
cd src
dotnet user-secrets set "MoMoPayment:SecretKey" "YOUR_SECRET_KEY"
dotnet user-secrets set "MoMoPayment:AccessKey" "YOUR_ACCESS_KEY"
```

## 9. Tài liệu tham khảo

- **MoMo Business**: https://business.momo.vn/
- **MoMo Developers**: https://developers.momo.vn/
- **GitHub Examples**: https://github.com/momo-wallet/payment
- **Support**: support@momo.vn, 1900 545426

## 10. Lưu ý quan trọng

### Signature Encoding
- Sử dụng **UTF-8 encoding** cho signature
- Code: `Encoding.UTF8.GetBytes(text)` và `Encoding.UTF8.GetBytes(key)`

### Thứ tự fields trong signature
```
partnerCode={partnerCode}&accessKey={accessKey}&requestId={requestId}&amount={amount}&orderId={orderId}&orderInfo={orderInfo}&returnUrl={returnUrl}&notifyUrl={notifyUrl}&extraData={extraData}&storeId={storeId}
```

### IPN Callback
- MoMo sẽ gọi IPN URL sau khi thanh toán
- Cần verify signature từ callback data
- Luôn trả về HTTP 200 OK