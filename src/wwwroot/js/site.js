// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Audio Upload Handler
class AudioUploader {
    constructor() {
        this.selectedFile = null;
        this.init();
    }

    init() {
        const dropZone = $('#dropZone');
        const fileInput = $('#audioFile');

        dropZone.on('click', () => fileInput.click());

        dropZone.on('dragover', (e) => {
            e.preventDefault();
            dropZone.css('background', 'var(--blue-100)');
        });

        dropZone.on('dragleave', (e) => {
            e.preventDefault();
            dropZone.css('background', 'var(--blue-light)');
        });

        dropZone.on('drop', (e) => {
            e.preventDefault();
            dropZone.css('background', 'var(--blue-light)');
            const files = e.originalEvent.dataTransfer.files;
            if (files.length > 0) {
                this.handleFile(files[0]);
            }
        });

        fileInput.on('change', (e) => {
            if (e.target.files.length > 0) {
                this.handleFile(e.target.files[0]);
            }
        });

        $('#removeFile').on('click', () => this.removeFile());

        $('#uploadBtn').on('click', () => this.upload());
    }

    handleFile(file) {
        const allowedTypes = ['audio/mpeg', 'audio/wav', 'audio/x-m4a', 'audio/mp4'];
        if (!allowedTypes.includes(file.type) && !file.name.match(/\.(mp3|wav|m4a)$/i)) {
            alert('Chỉ hỗ trợ file MP3, WAV, M4A!');
            return;
        }

        if (file.size > 20 * 1024 * 1024) {
            alert('File quá lớn! Tối đa 20MB.');
            return;
        }

        this.selectedFile = file;

        $('#fileName').text(file.name);
        $('#fileSize').text((file.size / (1024 * 1024)).toFixed(2) + ' MB');
        $('#filePreview').removeClass('d-none');
        $('#dropZone').addClass('d-none');
    }

    removeFile() {
        this.selectedFile = null;
        $('#audioFile').val('');
        $('#filePreview').addClass('d-none');
        $('#dropZone').removeClass('d-none');
        $('#progressContainer').addClass('d-none');
        $('#progressBar').css('width', '0%');
        $('#progressText').text('');
    }

    upload() {
        if (!this.selectedFile) return;

        const formData = new FormData();
        formData.append('file', this.selectedFile);
        formData.append('sourceLanguage', $('#sourceLanguage').val());
        formData.append('targetLanguage', $('#targetLanguage').val());

        $('#progressContainer').removeClass('d-none');
        $('#uploadBtn').prop('disabled', true);
        $('#globalLoading').fadeIn(150);

        // Real AJAX upload
        $.ajax({
            url: '/Audio/Upload',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            xhr: function () {
                const xhr = new window.XMLHttpRequest();
                xhr.upload.addEventListener('progress', function (e) {
                    if (e.lengthComputable) {
                        const percent = Math.round((e.loaded / e.total) * 100);
                        $('#progressBar').css('width', percent + '%');
                        $('#progressText').text('Đang tải lên... ' + percent + '%');
                    }
                });
                return xhr;
            },
            success: function (response) {
                if (response.success) {
                    $('#progressText').text('✓ Hoàn thành! Đang chuyển hướng...');
                    setTimeout(() => {
                        window.location.href = '/Audio/Processing/' + response.audioFileId;
                    }, 1500);
                } else {
                    alert(response.message || 'Không thể xử lý file. Vui lòng thử lại.');
                    $('#uploadBtn').prop('disabled', false);
                    $('#progressContainer').addClass('d-none');
                    $('#progressBar').css('width', '0%');
                    $('#progressText').text('');
                }
                $('#globalLoading').fadeOut(150);
            },
            error: function (xhr, status, error) {
                alert('Lỗi: ' + (xhr.responseJSON?.message || 'Không thể tải lên file'));
                $('#uploadBtn').prop('disabled', false);
                $('#progressContainer').addClass('d-none');
                $('#progressBar').css('width', '0%');
                $('#progressText').text('');
                $('#globalLoading').fadeOut(150);
            }
        });
    }
}

class FormValidator {
    static validateEmail(email) {
        const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return regex.test(email);
    }

    static validatePassword(password, minLength = 6) {
        return password.length >= minLength;
    }

    static matchPasswords(password, confirmPassword) {
        return password === confirmPassword;
    }
}

// Initialize on document ready
$(document).ready(function () {
    $('.alert-dismissible').delay(5000).fadeOut('slow');

    let isUserAction = false;
    
    $(document).on('click', 'button, a, input[type="submit"]', function() {
        if (!$(this).hasClass('no-loading')) {
            isUserAction = true;
        }
    });
    
    $(document).on('submit', 'form', function() {
        isUserAction = true;
    });
    
    $(document).ajaxStart(function () {
        if (isUserAction) {
            $('#globalLoading').fadeIn(150);
        }
    });

    $(document).ajaxStop(function () {
        $('#globalLoading').fadeOut(150);
        isUserAction = false;
    });

    if ($('#dropZone').length) {
        new AudioUploader();
    }

    $('#loginForm').on('submit', function (e) {
        let isValid = true;

        const email = $('#email').val();
        if (!FormValidator.validateEmail(email)) {
            $('#email').addClass('is-invalid');
            isValid = false;
        } else {
            $('#email').removeClass('is-invalid');
        }

        const password = $('#password').val();
        if (password.length === 0) {
            $('#password').addClass('is-invalid');
            isValid = false;
        } else {
            $('#password').removeClass('is-invalid');
        }

        if (!isValid) {
            e.preventDefault();
        }
    });

    $('#registerForm').on('submit', function (e) {
        let isValid = true;

        const displayName = $('#displayName').val();
        if (displayName.length < 3) {
            $('#displayName').addClass('is-invalid');
            isValid = false;
        } else {
            $('#displayName').removeClass('is-invalid');
        }

        const email = $('#email').val();
        if (!FormValidator.validateEmail(email)) {
            $('#email').addClass('is-invalid');
            isValid = false;
        } else {
            $('#email').removeClass('is-invalid');
        }

        const password = $('#password').val();
        if (!FormValidator.validatePassword(password, 6)) {
            $('#password').addClass('is-invalid');
            isValid = false;
        } else {
            $('#password').removeClass('is-invalid');
        }

        const confirmPassword = $('#confirmPassword').val();
        if (!FormValidator.matchPasswords(password, confirmPassword)) {
            $('#confirmPassword').addClass('is-invalid');
            isValid = false;
        } else {
            $('#confirmPassword').removeClass('is-invalid');
        }

        if (!isValid) {
            e.preventDefault();
        }
    });

    $('#confirmPassword').on('keyup', function () {
        const password = $('#password').val();
        const confirmPassword = $(this).val();

        if (!FormValidator.matchPasswords(password, confirmPassword)) {
            $(this).addClass('is-invalid');
        } else {
            $(this).removeClass('is-invalid');
        }
    });

    $('#togglePassword').on('click', function () {
        const passwordInput = $('#password');
        const icon = $('#togglePasswordIcon, #toggleIcon');

        if (passwordInput.attr('type') === 'password') {
            passwordInput.attr('type', 'text');
            icon.removeClass('bi-eye').addClass('bi-eye-slash');
        } else {
            passwordInput.attr('type', 'password');
            icon.removeClass('bi-eye-slash').addClass('bi-eye');
        }
    });

    $('#toggleConfirmPassword').on('click', function () {
        const confirmPasswordInput = $('#confirmPassword');
        const icon = $('#toggleConfirmPasswordIcon');

        if (confirmPasswordInput.attr('type') === 'password') {
            confirmPasswordInput.attr('type', 'text');
            icon.removeClass('bi-eye').addClass('bi-eye-slash');
        } else {
            confirmPasswordInput.attr('type', 'password');
            icon.removeClass('bi-eye-slash').addClass('bi-eye');
        }
    });
});