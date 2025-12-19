// Toast notification system
function showToast(message, type = "info") {
  const toastContainer = getOrCreateToastContainer();
  const toastId = "toast-" + Date.now();
  const bgColor =
    type === "success"
      ? "bg-success"
      : type === "error"
      ? "bg-danger"
      : "bg-primary";

  const toastHTML = `
        <div class="toast ${bgColor} text-white" id="${toastId}" role="alert">
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;

  toastContainer.insertAdjacentHTML("beforeend", toastHTML);
  const toastElement = new bootstrap.Toast(document.getElementById(toastId), {
    delay: 2000,
  });
  toastElement.show();

  // Remove toast after it's hidden
  document.getElementById(toastId).addEventListener("hidden.bs.toast", () => {
    document.getElementById(toastId).remove();
  });
}

function getOrCreateToastContainer() {
  let container = document.querySelector(".toast-container");
  if (!container) {
    container = document.createElement("div");
    container.className = "toast-container position-fixed top-0 end-0 p-3";
    document.body.appendChild(container);
  }
  return container;
}

// Show/hide sections
function showSection(sectionId) {
  document.querySelectorAll(".form-section").forEach((section) => {
    section.classList.remove("active");
  });
  const targetSection = document.getElementById(sectionId);
  if (targetSection) {
    targetSection.classList.add("active");
  }
}

// Password visibility toggles
function setupPasswordToggle(inputId, buttonId) {
  const input = document.getElementById(inputId);
  const button = document.getElementById(buttonId);

  if (input && button) {
    button.addEventListener("click", () => {
      const type =
        input.getAttribute("type") === "password" ? "text" : "password";
      input.setAttribute("type", type);
      button.querySelector("i").className =
        type === "password" ? "bi bi-eye" : "bi bi-eye-slash";
    });
  }
}

// Initialize password toggles
setupPasswordToggle("loginPassword", "toggleLoginPassword");
setupPasswordToggle("registerPassword", "toggleRegisterPassword");
setupPasswordToggle("NewPassword", "toggleNewPassword");
setupPasswordToggle("ConfirmPassword", "toggleConfirmPassword");

// Confirm logout functionality
function confirmLogout() {
  // Tắt xác nhận đăng xuất, submit luôn form nếu có
  const logoutForm = document.querySelector('form[action*="DangXuat"]');
  if (logoutForm) {
    logoutForm.submit();
  } else {
    // Fallback: create and submit form
    const form = document.createElement("form");
    form.method = "POST";
    form.action = "/DangNhap/DangXuat";

    // Add anti-forgery token
    const token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    );
    if (token) {
      const hiddenToken = document.createElement("input");
      hiddenToken.type = "hidden";
      hiddenToken.name = "__RequestVerificationToken";
      hiddenToken.value = token.value;
      form.appendChild(hiddenToken);
    }

    document.body.appendChild(form);
    form.submit();
  }
}

// Initialize on DOM load
document.addEventListener("DOMContentLoaded", () => {
  // Set up logout button click handler
  const logoutButtons = document.querySelectorAll('button[type="submit"]');
  logoutButtons.forEach((button) => {
    if (button.textContent.includes("Đăng Xuất")) {
      button.addEventListener("click", (e) => {
        e.preventDefault();
        confirmLogout();
      });
    }
  });
});
//         });

//         // Random twinkle effect
//         if (Math.random() > 0.7) {
//             setInterval(() => {
//                 icon.style.opacity = Math.random() * 0.3 + 0.1;
//                 setTimeout(() => {
//                     icon.style.opacity = '';
//                 }, 200);
//             }, Math.random() * 3000 + 2000);
//         }
//     });
// }

// // Test avatar function (simplified)
// function testAvatarFunction() {
//     const avatarInput = document.getElementById('avatarInput');
//     if (avatarInput) {
//         avatarInput.click();
//         showToast('Hộp thoại chọn file đã mở!', 'success');
//     } else {
//         // Create input if not found
//         ensureAvatarInput();
//         const newInput = document.getElementById('avatarInput');
//         if (newInput) {
//             newInput.click();
//             showToast('Hộp thoại chọn file đã mở!', 'success');
//         } else {
//             showToast('Không thể tạo input file!', 'error');
//         }
//     }
// }

// // Ensure avatar input exists
// function ensureAvatarInput() {
//     let avatarInput = document.getElementById('avatarInput');
//     const avatarElement = document.getElementById('profileAvatar');

//     if (!avatarInput && avatarElement) {
//         // Create the input
//         avatarInput = document.createElement('input');
//         avatarInput.type = 'file';
//         avatarInput.id = 'avatarInput';
//         avatarInput.accept = 'image/*';
//         avatarInput.style.display = 'none';
//         avatarInput.onchange = handleAvatarChange;

//         // Add to avatar element
//         avatarElement.appendChild(avatarInput);
//     }

//     return avatarInput;
// }

// // Initialize all background effects
// document.addEventListener('DOMContentLoaded', () => {
//     setTimeout(() => {
//         createDynamicParticles();
//         addMouseInteraction();
//         addColorPulse();
//         enhanceFloatingIcons();
//         loadSavedAvatar(); // Load saved avatar when page loads

//         // Ensure avatar input exists after page load
//         ensureAvatarInput();
//     }, 1000);
// });

// Show/hide sections
function showSection(sectionId) {
  document.querySelectorAll(".form-section").forEach((section) => {
    section.classList.remove("active");
  });
  document.getElementById(sectionId).classList.add("active");
}

// // Dashboard tabs
// function showDashboardTab(tabId) {
//     document.querySelectorAll('.dashboard-tab').forEach(tab => {
//         tab.style.display = 'none';
//     });
//     document.getElementById(tabId).style.display = 'block';

//     // Update sidebar nav
//     document.querySelectorAll('.sidebar .nav-link').forEach(link => {
//         link.classList.remove('active');
//     });
//     event.target.classList.add('active');
// }

// Password strength checker
function checkPasswordStrength(password, barId, textId) {
  const bar = document.getElementById(barId);
  const text = document.getElementById(textId);

  if (password.length === 0) {
    bar.style.width = "0%";
    bar.className = "strength-bar";
    text.textContent = "Nhập mật khẩu để kiểm tra độ mạnh";
    text.className = "text-muted";
    return;
  }

  let score = 0;
  let feedback = [];

  // Kiểm tra độ dài
  if (password.length >= 8) {
    score++;
  } else {
    feedback.push("ít nhất 8 ký tự");
  }

  // Kiểm tra chữ thường
  if (/[a-z]/.test(password)) {
    score++;
  } else {
    feedback.push("chữ thường");
  }

  // Kiểm tra chữ hoa
  if (/[A-Z]/.test(password)) {
    score++;
  } else {
    feedback.push("chữ hoa");
  }

  // Kiểm tra số
  if (/[0-9]/.test(password)) {
    score++;
  } else {
    feedback.push("số");
  }

  // Kiểm tra ký tự đặc biệt
  if (/[^A-Za-z0-9]/.test(password)) {
    score++;
  } else {
    feedback.push("ký tự đặc biệt");
  }

  // Cập nhật thanh độ mạnh và text
  if (score <= 2) {
    bar.style.width = "33%";
    bar.className = "strength-bar strength-weak";
    text.textContent = `Mật khẩu yếu - Cần: ${feedback.slice(0, 2).join(", ")}`;
    text.className = "text-danger";
  } else if (score <= 3) {
    bar.style.width = "66%";
    bar.className = "strength-bar strength-medium";
    text.textContent =
      feedback.length > 0
        ? `Mật khẩu trung bình - Cần: ${feedback.join(", ")}`
        : "Mật khẩu trung bình";
    text.className = "text-warning";
  } else {
    bar.style.width = "100%";
    bar.className = "strength-bar strength-strong";
    text.textContent = "Mật khẩu mạnh";
    text.className = "text-success";
  }
}

// Password visibility toggles
function setupPasswordToggle(inputId, buttonId) {
  const input = document.getElementById(inputId);
  const button = document.getElementById(buttonId);

  if (input && button) {
    button.addEventListener("click", () => {
      const type =
        input.getAttribute("type") === "password" ? "text" : "password";
      input.setAttribute("type", type);
      button.querySelector("i").className =
        type === "password" ? "bi bi-eye" : "bi bi-eye-slash";
    });
  }
}

// Password strength monitoring - Cập nhật phần này
document.addEventListener("DOMContentLoaded", () => {
  // Kiểm tra độ mạnh mật khẩu cho form đăng ký
  const registerPassword = document.getElementById("registerPassword");
  if (registerPassword) {
    registerPassword.addEventListener("input", (e) => {
      checkPasswordStrength(e.target.value, "strengthBar", "strengthText");
    });
  }

  // Kiểm tra xác nhận mật khẩu
  const confirmPassword = document.getElementById("confirmPassword");
  if (confirmPassword && registerPassword) {
    confirmPassword.addEventListener("input", (e) => {
      checkPasswordMatch(registerPassword.value, e.target.value);
    });

    // Cũng kiểm tra khi mật khẩu chính thay đổi
    registerPassword.addEventListener("input", (e) => {
      if (confirmPassword.value) {
        checkPasswordMatch(e.target.value, confirmPassword.value);
      }
    });
  }

  // Kiểm tra độ mạnh mật khẩu cho form reset password (Doi_MK)
  const newPassword = document.getElementById("NewPassword");
  if (newPassword) {
    newPassword.addEventListener("input", (e) => {
      checkPasswordStrength(
        e.target.value,
        "resetStrengthBar",
        "resetStrengthText"
      );
    });
  }

  // Kiểm tra xác nhận mật khẩu cho form reset password
  const confirmNewPassword = document.getElementById("ConfirmPassword");
  if (confirmNewPassword && newPassword) {
    confirmNewPassword.addEventListener("input", (e) => {
      checkResetPasswordMatch(newPassword.value, e.target.value);
    });

    // Cũng kiểm tra khi mật khẩu mới thay đổi
    newPassword.addEventListener("input", (e) => {
      if (confirmNewPassword.value) {
        checkResetPasswordMatch(e.target.value, confirmNewPassword.value);
      }
    });
  }
});

// Thêm hàm kiểm tra khớp mật khẩu
function checkPasswordMatch(password, confirmPassword) {
  const confirmInput = document.getElementById("confirmPassword");
  const indicator =
    document.getElementById("passwordMatchIndicator") ||
    createPasswordMatchIndicator();

  if (confirmPassword.length === 0) {
    indicator.style.display = "none";
    confirmInput.classList.remove("is-valid", "is-invalid");
    return;
  }

  if (password === confirmPassword) {
    indicator.style.display = "block";
    indicator.className = "text-success mt-1";
    indicator.innerHTML =
      '<i class="bi bi-check-circle me-1"></i>Mật khẩu khớp';
    confirmInput.classList.remove("is-invalid");
    confirmInput.classList.add("is-valid");
  } else {
    indicator.style.display = "block";
    indicator.className = "text-danger mt-1";
    indicator.innerHTML =
      '<i class="bi bi-x-circle me-1"></i>Mật khẩu không khớp';
    confirmInput.classList.remove("is-valid");
    confirmInput.classList.add("is-invalid");
  }
}

function createPasswordMatchIndicator() {
  const confirmInput = document.getElementById("confirmPassword");
  const indicator = document.createElement("div");
  indicator.id = "passwordMatchIndicator";
  indicator.style.display = "none";
  confirmInput.parentNode.appendChild(indicator);
  return indicator;
}

// Thêm hàm kiểm tra khớp mật khẩu cho form reset password
function checkResetPasswordMatch(password, confirmPassword) {
  const confirmInput = document.getElementById("ConfirmPassword");
  const indicator =
    document.getElementById("confirmPasswordMatchIndicator") ||
    createResetPasswordMatchIndicator();

  if (confirmPassword.length === 0) {
    indicator.style.display = "none";
    confirmInput.classList.remove("is-valid", "is-invalid");
    return;
  }

  if (password === confirmPassword) {
    indicator.style.display = "block";
    indicator.className = "text-success mt-1";
    indicator.innerHTML =
      '<i class="bi bi-check-circle me-1"></i>Mật khẩu khớp';
    confirmInput.classList.remove("is-invalid");
    confirmInput.classList.add("is-valid");
  } else {
    indicator.style.display = "block";
    indicator.className = "text-danger mt-1";
    indicator.innerHTML =
      '<i class="bi bi-x-circle me-1"></i>Mật khẩu không khớp';
    confirmInput.classList.remove("is-valid");
    confirmInput.classList.add("is-invalid");
  }
}

function createResetPasswordMatchIndicator() {
  const confirmInput = document.getElementById("ConfirmPassword");
  const indicator = document.createElement("div");
  indicator.id = "confirmPasswordMatchIndicator";
  indicator.style.display = "none";
  confirmInput.parentNode.appendChild(indicator);
  return indicator;
}

// Cập nhật hàm checkPasswordStrength để hoạt động tốt hơn
function checkPasswordStrength(password, barId, textId) {
  const bar = document.getElementById(barId);
  const text = document.getElementById(textId);

  if (!bar || !text) return;

  if (password.length === 0) {
    bar.style.width = "0%";
    bar.className = "strength-bar";
    text.textContent = "Nhập mật khẩu để kiểm tra độ mạnh";
    text.className = "text-muted";
    return;
  }

  let score = 0;
  let feedback = [];

  // Kiểm tra độ dài
  if (password.length >= 8) {
    score++;
  } else {
    feedback.push("ít nhất 4 ký tự");
  }

  // Kiểm tra chữ thường
  if (/[a-z]/.test(password)) {
    score++;
  } else {
    feedback.push("chữ thường");
  }

  // Kiểm tra chữ hoa
  if (/[A-Z]/.test(password)) {
    score++;
  } else {
    feedback.push("chữ hoa");
  }

  // Kiểm tra số
  if (/[0-9]/.test(password)) {
    score++;
  } else {
    feedback.push("số");
  }

  // Kiểm tra ký tự đặc biệt
  if (/[^A-Za-z0-9]/.test(password)) {
    score++;
  } else {
    feedback.push("ký tự đặc biệt");
  }

  // Cập nhật thanh độ mạnh và text
  if (score <= 2) {
    bar.style.width = "33%";
    bar.className = "strength-bar strength-weak";
    text.textContent = `Mật khẩu yếu - Cần: ${feedback.slice(0, 2).join(", ")}`;
    text.className = "text-danger";
  } else if (score <= 3) {
    bar.style.width = "66%";
    bar.className = "strength-bar strength-medium";
    text.textContent =
      feedback.length > 0
        ? `Mật khẩu trung bình - Cần: ${feedback.join(", ")}`
        : "Mật khẩu trung bình";
    text.className = "text-warning";
  } else {
    bar.style.width = "100%";
    bar.className = "strength-bar strength-strong";
    text.textContent = "Mật khẩu mạnh";
    text.className = "text-success";
  }
}

// Password visibility toggles
function setupPasswordToggle(inputId, buttonId) {
  const input = document.getElementById(inputId);
  const button = document.getElementById(buttonId);

  if (input && button) {
    button.addEventListener("click", () => {
      const type =
        input.getAttribute("type") === "password" ? "text" : "password";
      input.setAttribute("type", type);
      button.querySelector("i").className =
        type === "password" ? "bi bi-eye" : "bi bi-eye-slash";
    });
  }
}

// // Avatar change handler
// function handleAvatarChange(event) {
//     const file = event.target.files[0];
//     if (file) {
//         // Validate file type
//         if (!file.type.startsWith('image/')) {
//             showToast('Vui lòng chọn file ảnh hợp lệ!', 'error');
//             return;
//         }

//         // Validate file size (max 5MB)
//         if (file.size > 5 * 1024 * 1024) {
//             showToast('Kích thước ảnh không được vượt quá 5MB!', 'error');
//             return;
//         }

//         // Open crop modal
//         openCropModal(file)
//             .then(croppedDataUrl => {
//                 // Apply the cropped image to avatar
//                 updateAvatarWithImage(croppedDataUrl);
//                 localStorage.setItem('userAvatar', croppedDataUrl);
//                 showToast('Avatar đã được cập nhật thành công!', 'success');
//             })
//             .catch(error => {
//                 console.error('Lỗi khi crop image:', error);
//                 showToast('Có lỗi xảy ra khi cắt ảnh!', 'error');
//             });
//     }
// }
document.addEventListener("DOMContentLoaded", function () {
  const passwordInput = document.getElementById("registerPassword");
  const confirmPasswordInput = document.getElementById("confirmPassword");
  const warningDiv = document.getElementById("passwordMismatchWarning");
  const submitButton = document.querySelector('button[type="submit"]');

  function checkPasswordMatch() {
    const password = passwordInput.value;
    const confirmPassword = confirmPasswordInput.value;

    if (confirmPassword.length > 0) {
      if (password !== confirmPassword) {
        warningDiv.style.display = "block";
        confirmPasswordInput.classList.add("is-invalid");
        submitButton.disabled = true;
      } else {
        warningDiv.style.display = "none";
        confirmPasswordInput.classList.remove("is-invalid");
        submitButton.disabled = false;
      }
    } else {
      warningDiv.style.display = "none";
      confirmPasswordInput.classList.remove("is-invalid");
      submitButton.disabled = false;
    }
  }

  confirmPasswordInput.addEventListener("input", checkPasswordMatch);
  passwordInput.addEventListener("input", checkPasswordMatch);
});
