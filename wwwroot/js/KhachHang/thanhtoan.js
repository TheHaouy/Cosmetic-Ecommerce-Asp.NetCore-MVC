const addAddressBtn = document.getElementById('addAddressBtn');
const addressModal = document.getElementById('addressModal');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelAddressBtn = document.getElementById('cancelAddressBtn');
const saveAddressBtn = document.getElementById('saveAddressBtn');
const addressContainer = document.getElementById('addressList');

// 5. STRIPE INTEGRATION - KHỞI TẠO
// DÁN KHÓA CÔNG KHAI 
const STRIPE_PUBLISHABLE_KEY = "pk_test_51SOKIpE8Ov10xXFiVQi5EBdBvDMdlUnlHIAfSYtnh4LZ2owumfuyU6lc4mohsGCpmzPvR4SA2y618Gy5eJ7CQIUh002E7KkGEW";

// Khởi tạo Stripe
let stripe;
let elements;
let currentStripeClientSecret = null;
let currentStripePaymentIntentId = null; // Lưu PaymentIntent ID để track

if (STRIPE_PUBLISHABLE_KEY && STRIPE_PUBLISHABLE_KEY.startsWith("pk_test_")) {
    stripe = Stripe(STRIPE_PUBLISHABLE_KEY);
} else {
    console.error("Stripe Publishable Key chưa được thiết lập hoặc không hợp lệ!");
    // Cân nhắc hiển thị lỗi cho người dùng ở đây
}

// HẾT PHẦN KHỞI TẠO STRIPE

// Biến lưu trữ autocomplete controllers
let provinceAutocomplete = null;
let communeAutocomplete = null;

// Đảm bảo modal ẩn khi trang load
document.addEventListener('DOMContentLoaded', function() {
    console.log('=== THANH TOÁN JS LOADED ===', new Date().toISOString());
    
    if (addressModal) {
        addressModal.classList.add('d-none');
        addressModal.classList.remove('d-flex');
    }
    
    // Load địa chỉ từ database khi trang được tải
    loadAddressesFromDatabase();
    
    // Load provinces data
    loadProvinces();
    
    setTimeout(() => {
        console.log('Initializing order display. isBuyNow:', window.isBuyNow);
        initializeOrderDisplay();
    }, 100);

    setupPaymentMethodToggle(); // Gọi hàm cài đặt ẩn/hiện

    // Thêm sự kiện click cho nút "Thanh toán Thẻ" (nút mới của Stripe)
    const stripeSubmitBtn = document.getElementById('stripe-submit-button');
    if (stripeSubmitBtn) {
        stripeSubmitBtn.addEventListener('click', handleStripeSubmit);
    }
    
    // Setup autocomplete for address inputs - use correct IDs
    setupAddressAutocomplete('inputCity', 'cityDropdown', 'province');
    setupAddressAutocomplete('inputWard', 'wardDropdown', 'commune');
});

// Hàm đóng modal thành công - định nghĩa ngay từ đầu
function closeSuccessModal() {
  console.log('Đang đóng modal thành công...');
  const successModal = document.getElementById('paymentSuccessModal');
  if (successModal) {
    successModal.classList.remove('d-flex');
    successModal.classList.add('d-none');
    
    // Clear tất cả dữ liệu localStorage liên quan đến giỏ hàng (chỉ khi không phải "mua ngay")
    if (!window.isBuyNow) {
      localStorage.removeItem('orderItems');
      localStorage.removeItem('cartSelected');
      localStorage.removeItem('selectedCartItems');
    }
    
    // Chuyển hướng khác nhau tùy theo chế độ
    console.log('Đang chuyển hướng...');
    setTimeout(function() {
      if (window.isBuyNow) {
        // Nếu là "mua ngay", chuyển về trang chủ hoặc trang sản phẩm
        window.location.href = '/KhachHang/SanPham';
      } else {
        // Nếu là giỏ hàng thông thường, chuyển về giỏ hàng với refresh
        window.location.href = '/KhachHang/Cart?refresh=true';
      }
    }, 300);
  } else {
    console.error('Không tìm thấy modal thành công');
  }
}

// Hàm cập nhật số lượng giỏ hàng sau khi thanh toán thành công
async function updateCartCountAfterPayment() {
  try {
    console.log('Đang cập nhật số lượng giỏ hàng...');
    const response = await fetch('/KhachHang/Cart/GetCartCount', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    if (response.ok) {
      const result = await response.json();
      const cartCountElement = document.getElementById('cartCount');
      
      if (cartCountElement) {
        if (result.cartCount > 0) {
          cartCountElement.textContent = result.cartCount;
          cartCountElement.style.display = '';
        } else {
          cartCountElement.style.display = 'none';
        }
        console.log(`Đã cập nhật số lượng giỏ hàng: ${result.cartCount}`);
      }
    } else {
      console.error('Lỗi khi lấy số lượng giỏ hàng:', response.status);
    }
  } catch (error) {
    console.error('Lỗi khi cập nhật số lượng giỏ hàng:', error);
  }
}

// Hiển thị modal khi click nút "Thêm địa chỉ giao hàng"
addAddressBtn?.addEventListener('click', () => {
    addressModal.classList.remove('d-none');
    addressModal.classList.add('d-flex');
    
    // Reset form
    const form = document.getElementById('addressForm');
    if (form) {
        form.reset();
    }
    
    // Khởi tạo autocomplete cho địa chỉ (sử dụng address-api.js)
    const cityInput = document.getElementById('inputCity');
    const cityDropdown = document.getElementById('cityDropdown');
    const wardInput = document.getElementById('inputWard');
    const wardDropdown = document.getElementById('wardDropdown');
    
    if (cityInput && cityDropdown && !provinceAutocomplete) {
        console.log('[Payment] Initializing province autocomplete...');
        provinceAutocomplete = initProvinceAutocomplete(
            cityInput,
            cityDropdown,
            (province) => {
                console.log('[Payment] Province selected:', province.name);
                // Reset ward input khi đổi tỉnh
                if (wardInput) {
                    wardInput.value = '';
                    wardInput.dataset.communeId = '';
                }
            }
        );
    }
    
    if (wardInput && wardDropdown && cityInput && !communeAutocomplete) {
        console.log('[Payment] Initializing commune autocomplete...');
        communeAutocomplete = initCommuneAutocomplete(
            wardInput,
            wardDropdown,
            cityInput,
            (commune) => {
                console.log('[Payment] Commune selected:', commune.name);
            }
        );
    }
});

// Hàm tạo dropdown elements nếu chưa có
// Ẩn modal khi click nút đóng hoặc hủy
[closeModalBtn, cancelAddressBtn].forEach(btn => {
    btn?.addEventListener('click', () => {
        addressModal.classList.remove('d-flex');
        addressModal.classList.add('d-none');
    });
});

// Alert Modal System - Simple implementation using existing modal
function showAlertModal(title, message, icon = 'fas fa-exclamation-circle') {
    document.getElementById('alertModalTitle').textContent = title;
    document.getElementById('alertModalMessage').textContent = message;
    document.getElementById('alertModalIcon').className = icon;

    document.getElementById('alertModal').classList.remove('d-none');
    document.getElementById('alertModal').classList.add('d-flex');
}

function closeAlertModal() {
    const alertModal = document.getElementById('alertModal');
    alertModal.classList.remove('d-flex');
    alertModal.classList.add('d-none');
}

// Kiểm tra họ và tên: tối đa 50 ký tự, không ký tự đặc biệt, chữ cái đầu viết hoa, tiếng Việt có dấu
function validateFullName(name) {
  name = name.trim();
  if (name.length === 0) {
    showAlertModal("Lỗi Nhập Liệu", "Vui lòng nhập họ và tên", "fas fa-exclamation-triangle");
    return false;
  }
  if (name.length > 50) {
    showAlertModal("Lỗi Nhập Liệu", "Họ và tên không được vượt quá 50 ký tự", "fas fa-exclamation-triangle");
    return false;
  }
  // Cho phép chữ hoa tiếng Việt đầu từ, các ký tự còn lại là thường hoặc dấu nháy đơn, nhiều từ cách nhau bởi 1 khoảng trắng
  const regex = /^([A-ZÀÁÂÃÄÅĀĂĄẠẢẤẦẨẪẬẮẰẲẴẶÆÇĆĈĊČĐÐÈÉÊËĒĔĖĘĚẸẺẼẾỀỂỄỆÌÍÎÏĨĪĮİỈỊÑÒÓÔÕÖØŌŎŐƠỌỎỐỒỔỖỘỚỜỞỠỢÙÚÛÜŨŪŬŮŰŲƯỤỦỨỪỬỮỰÝŸỲỴỶỸ][a-zàáâãäåāăąạảấầẩẫậắằẳẵặæçćĉċčđðèéêëēĕėęěẹẻẽếềểễệìíîïĩīįiỉịñòóôõöøōŏőơọỏốồổỗộớờởỡợùúûüũūŭůűųưụủứừửữựýÿỳỵỷỹ']*)(\s([A-ZÀÁÂÃÄÅĀĂĄẠẢẤẦẨẪẬẮẰẲẴẶÆÇĆĈĊČĐÐÈÉÊËĒĔĖĘĚẸẺẼẾỀỂỄỆÌÍÎÏĨĪĮİỈỊÑÒÓÔÕÖØŌŎŐƠỌỎỐỒỔỖỘỚỜỞỠỢÙÚÛÜŨŪŬŮŰŲƯỤỦỨỪỬỮỰÝŸỲỴỶỸ][a-zàáâãäåāăąạảấầẩẫậắằẳẵặæçćĉċčđðèéêëēĕėęěẹẻẽếềểễệìíîïĩīįiỉịñòóôõöøōŏőơọỏốồổỗộớờởỡợùúûüũūŭůűųưụủứừửữựýÿỳỵỷỹ']*))*$/u;
  if (!regex.test(name)) {
    showAlertModal("Định Dạng Không Hợp Lệ", "Họ và tên phải viết hoa chữ cái đầu mỗi từ và không chứa ký tự đặc biệt", "fas fa-exclamation-triangle");
    return false;
  }
  return true;
}

// Kiểm tra số điện thoại: bắt đầu bằng 0, 10 chữ số, chỉ số
function validatePhoneNumber(phone) {
  phone = phone.trim();
  if (phone.length === 0) {
    showAlertModal("Lỗi Nhập Liệu", "Vui lòng nhập số điện thoại", "fas fa-exclamation-triangle");
    return false;
  }
  if (!/^0\d{9}$/.test(phone)) {
    showAlertModal("Số Điện Thoại Không Hợp Lệ", "Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số", "fas fa-exclamation-triangle");
    return false;
  }
  return true;
}

// Load địa chỉ từ database
async function loadAddressesFromDatabase() {
    try {
        console.log('Đang load địa chỉ từ database...');
        
        // Hiển thị loading state
        showLoadingState();
        
        const response = await fetch('/KhachHang/Pay/GetAddressesForPayment', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            console.error('Lỗi HTTP:', response.status, response.statusText);
            showEmptyState();
            return;
        }

        const result = await response.json();
        console.log('Kết quả load địa chỉ:', result);
        
        if (result.success) {
            // Xóa tất cả địa chỉ hiện có trước khi load lại
            clearExistingAddresses();
            
            if (result.addresses && result.addresses.length > 0) {
                console.log(`Tìm thấy ${result.addresses.length} địa chỉ`);
                
                // Ẩn loading và empty state
                hideLoadingState();
                hideEmptyState();

                // Hiển thị các địa chỉ
                result.addresses.forEach((address, index) => {
                    console.log(`Đang thêm địa chỉ ${index + 1}:`, address);
                    addAddressCard(address);
                });

                // Tự động chọn địa chỉ mặc định
                const defaultAddress = addressContainer.querySelector('input[name="address"]:checked') || 
                                     addressContainer.querySelector('input[name="address"]');
                if (defaultAddress && !defaultAddress.checked) {
                    defaultAddress.checked = true;
                }
                
                console.log('Load địa chỉ thành công');
            } else {
                console.log('Không có địa chỉ nào được tìm thấy');
                hideLoadingState();
                showEmptyState();
            }
        } else {
            console.error('Lỗi từ server:', result.message);
            hideLoadingState();
            showEmptyState();
        }
    } catch (error) {
        console.error('Lỗi khi load địa chỉ:', error);
        hideLoadingState();
        showEmptyState();
    }
}

// Hàm xóa các địa chỉ hiện có
function clearExistingAddresses() {
    const existingAddresses = addressContainer.querySelectorAll('.address-card');
    existingAddresses.forEach(card => card.remove());
}

// Hàm hiển thị loading state
function showLoadingState() {
    const loadingElement = document.getElementById('loadingAddresses');
    const emptyElement = document.getElementById('emptyAddresses');
    if (loadingElement) {
        loadingElement.classList.remove('d-none');
        loadingElement.style.display = 'block';
    }
    if (emptyElement) {
        emptyElement.classList.add('d-none');
        emptyElement.style.display = 'none';
    }
}

// Hàm ẩn loading state
function hideLoadingState() {
    const loadingElement = document.getElementById('loadingAddresses');
    if (loadingElement) {
        loadingElement.classList.add('d-none');
        loadingElement.style.display = 'none';
    }
}

// Hàm hiển thị empty state
function showEmptyState() {
    const emptyElement = document.getElementById('emptyAddresses');
    if (emptyElement) {
        emptyElement.classList.remove('d-none');
        emptyElement.style.display = 'block';
    }
}

// Hàm ẩn empty state
function hideEmptyState() {
    const emptyElement = document.getElementById('emptyAddresses');
    if (emptyElement) {
        emptyElement.classList.add('d-none');
        emptyElement.style.display = 'none';
    }
}

// Thêm address card vào DOM
function addAddressCard(address) {
    const wrapper = document.createElement('div');
    wrapper.className = 'address-card p-4 rounded-3 mb-3 animate-fade-in position-relative';
    wrapper.dataset.addressId = address.id;
    
    wrapper.innerHTML = `
        <div class="d-flex justify-content-between align-items-start">
            <label class="d-flex align-items-start flex-grow-1 cursor-pointer">
                <input type="radio" name="address" value="${address.id}" class="form-check-input me-3 mt-1" style="transform: scale(1.2);" ${address.macDinh ? 'checked' : ''}>
                <div class="flex-grow-1">
                    <div class="address-details">
                        <div class="address-info">
                            <i class="fas fa-user-circle"></i>  
                            <span class="fw-bold">${address.hoTen || 'Không có tên'}</span>
                        </div>
                        <div class="address-info">
                            <i class="fas fa-phone"></i>
                            <span>${address.soDienThoai || 'Không có SĐT'}</span>
                        </div>
                        <div class="address-info">
                            <i class="fas fa-map-marker-alt"></i>
                            <span>${address.diaChiChiTiet || 'Không có địa chỉ'}</span>
                        </div>
                    </div>
                </div>
            </label>
            <button type="button" class="delete-btn delete-address-btn" data-address-id="${address.id}">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;
    
    addressContainer.appendChild(wrapper);
    setupAddressCardEvents();
}

saveAddressBtn?.addEventListener('click', async () => {
  const name = document.getElementById('inputName').value.trim();
  const phone = document.getElementById('inputPhone').value.trim();
  const address = document.getElementById('inputAddress').value.trim();
  const cityInput = document.getElementById('inputCity');
  const wardInput = document.getElementById('inputWard');
  const city = cityInput?.value.trim() || '';
  const ward = wardInput?.value.trim() || '';
  
  // Kiểm tra các trường bắt buộc cơ bản
  if (!name || !phone || !address) {
    showAlertModal("Thông Tin Chưa Đầy Đủ", "Vui lòng điền đầy đủ họ tên, số điện thoại và địa chỉ", "fas fa-exclamation-triangle");
    return;
  }
  
  // Kiểm tra bắt buộc phải chọn cả Tỉnh và Phường từ dropdown
  const citySelected = cityInput?.dataset.selected === 'true';
  const wardSelected = wardInput?.dataset.selected === 'true';
  
  if (!citySelected || !wardSelected) {
    showAlertModal("Thông Tin Chưa Đầy Đủ", "Vui lòng chọn đầy đủ cả Tỉnh/Thành phố và Phường/Xã từ danh sách gợi ý", "fas fa-exclamation-triangle");
    return;
  }

  if (!validateFullName(name)) return;
  if (!validatePhoneNumber(phone)) return;

  try {
    console.log('Đang lưu địa chỉ mới...');
    
    const requestData = {
      hoTen: name,
      soDienThoai: phone,
      diaChiChiTiet: address,
      tinhThanhPho: city,
      phuongXa: ward
    };
    
    console.log('Dữ liệu gửi lên server:', requestData);
    
    // Hiển thị loading state
    saveAddressBtn.disabled = true;
    saveAddressBtn.innerHTML = '<i class="fas fa-spinner fa-spin" style="margin-right: 6px;"></i> Đang lưu...';
    
    // Gửi request lên server để lưu địa chỉ
    const response = await fetch('/KhachHang/Pay/AddAddressFromPayment', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Requested-With': 'XMLHttpRequest'
      },
      body: JSON.stringify(requestData)
    });

    console.log('Response status:', response.status);
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    const result = await response.json();
    console.log('Response data:', result);
    
    if (result.success) {
      console.log('Địa chỉ đã được lưu thành công:', result.address);
      
      // Ẩn phần empty state khi có địa chỉ đầu tiên
      hideEmptyState();

      // Thêm địa chỉ mới vào DOM
      addAddressCard(result.address);

      // Tự động chọn địa chỉ vừa thêm
      const newRadio = addressContainer.querySelector(`input[value="${result.address.id}"]`);
      if (newRadio) {
        newRadio.checked = true;
        console.log('Đã chọn địa chỉ mới làm mặc định');
      }

      // Đóng modal và reset form
      addressModal.classList.remove('d-flex');
      addressModal.classList.add('d-none');
      
      // Reset form
      document.getElementById('addressForm').reset();
      
      // Refresh danh sách địa chỉ để đảm bảo đồng bộ với database
      setTimeout(() => {
        loadAddressesFromDatabase();
      }, 500);
      
      showAlertModal("Thành Công", "Địa chỉ đã được thêm thành công!", "fas fa-check-circle");
    } else {
      console.error('Lỗi từ server:', result.message);
      showAlertModal("Lỗi", result.message || "Có lỗi xảy ra khi thêm địa chỉ", "fas fa-exclamation-triangle");
    }
  } catch (error) {
    console.error('Lỗi khi lưu địa chỉ:', error);
    showAlertModal("Lỗi Kết Nối", "Không thể kết nối tới server. Vui lòng thử lại.", "fas fa-exclamation-triangle");
  } finally {
    // Reset button state
    saveAddressBtn.disabled = false;
    saveAddressBtn.innerHTML = '<i class="fas fa-save" style="margin-right: 6px;"></i> LƯU ĐỊA CHỈ';
  }
});

function setupAddressCardEvents() {
  const addressCards = document.querySelectorAll('.address-card');
  addressCards.forEach(card => {
    const radio = card.querySelector('input[type="radio"]');
    const deleteBtn = card.querySelector('.delete-address-btn');

    // Xóa event cũ để tránh duplicate
    card.onclick = null;
    if (deleteBtn) deleteBtn.onclick = null;

    card.addEventListener('click', () => {
      addressCards.forEach(c => {
        c.classList.remove('selected');
        c.querySelector('input[type="radio"]').checked = false;
      });
      card.classList.add('selected');
      radio.checked = true;
      
      // Lưu địa chỉ được chọn vào localStorage (optional)
      localStorage.setItem('selectedAddressId', radio.value);
      console.log('Đã chọn địa chỉ:', radio.value);
    });

    if (deleteBtn) {
      deleteBtn.addEventListener('click', async (e) => {
        e.stopPropagation();
        
        console.log('=== XÓA ĐỊA CHỈ - KHÔNG CÓ CONFIRM ===', new Date().toISOString());
        
        const addressId = deleteBtn.dataset.addressId;
        
        try {
          console.log('Đang xóa địa chỉ:', addressId);
          
          const response = await fetch('/KhachHang/DiaChi/DeleteAddress', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ id: parseInt(addressId) })
          });

          if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
          }

          const result = await response.json();
          
          if (result.success) {
            // Xóa địa chỉ được chọn khỏi localStorage nếu trùng
            const selectedAddressId = localStorage.getItem('selectedAddressId');
            if (selectedAddressId === addressId) {
              localStorage.removeItem('selectedAddressId');
            }
            
            // Reload danh sách địa chỉ để đảm bảo đồng bộ với database
            await loadAddressesFromDatabase();
            
            // Không hiển thị thông báo
            console.log('Đã xóa địa chỉ thành công');
          } else {
            console.error('Lỗi xóa địa chỉ từ server:', result.message);
            // Không hiển thị thông báo lỗi, chỉ log
            console.log('Không thể xóa địa chỉ:', result.message);
          }
        } catch (error) {
          console.error('Lỗi khi xóa địa chỉ:', error);
          // Không hiển thị thông báo lỗi, chỉ log
          console.log('Lỗi kết nối khi xóa địa chỉ');
        }
      });
    }
  });
  
  // Khôi phục địa chỉ được chọn từ localStorage (nếu có)
  const selectedAddressId = localStorage.getItem('selectedAddressId');
  if (selectedAddressId) {
    const targetRadio = document.querySelector(`input[name="address"][value="${selectedAddressId}"]`);
    if (targetRadio) {
      targetRadio.checked = true;
      targetRadio.closest('.address-card').classList.add('selected');
      console.log('Đã khôi phục địa chỉ được chọn:', selectedAddressId);
    }
  }
}

// Payment method selection
document.querySelectorAll('.payment-method').forEach(method => {
  method.addEventListener('click', () => {
    document.querySelectorAll('.payment-method').forEach(m => m.classList.remove('selected'));
    method.classList.add('selected');
    method.querySelector('input[type="radio"]').checked = true;
  });
});

// Xử lý nút quay lại giỏ hàng
const backToCartBtn = document.getElementById('backToCartBtn');
if (backToCartBtn) {
  backToCartBtn.addEventListener('click', () => {
    // Kiểm tra nếu là chế độ "mua ngay"
    if (window.isBuyNow) {
      history.back(); // Quay lại trang chi tiết sản phẩm
    } else {
      window.location.href = '/KhachHang/Cart'; // Quay lại giỏ hàng
    }
  });
}

// Xử lý hoàn tất đơn hàng
const completeOrderBtn = document.getElementById('completeOrderBtn');
if (completeOrderBtn) {
  completeOrderBtn.addEventListener('click', () => {
    const orderItems = getValidOrderItems(); // Sử dụng hàm getValidOrderItems để hỗ trợ cả chế độ mua ngay và giỏ hàng
    
    if (orderItems.length === 0) {
      const errorMessage = window.isBuyNow ? "Không thể xử lý sản phẩm mua ngay. Vui lòng thử lại!" : "Không có sản phẩm nào để thanh toán. Vui lòng thêm sản phẩm vào giỏ hàng!";
      showAlertModal("Giỏ Hàng Trống", errorMessage, "fas fa-exclamation-triangle");
      return;
    }

    // Kiểm tra xem có địa chỉ nào được thêm vào chưa bằng cách kiểm tra radio buttons
    const addressRadios = document.querySelectorAll('input[name="address"]');
    if (addressRadios.length === 0) {
      // Hiện modal thông báo chưa có địa chỉ
      const noAddressModal = document.getElementById('noAddressModal');
      noAddressModal.classList.remove('d-none');
      noAddressModal.classList.add('d-flex');
      return;
    }

    // Kiểm tra địa chỉ giao hàng đã được chọn chưa
    const selectedAddress = document.querySelector('input[name="address"]:checked');
    if (!selectedAddress) {
      showAlertModal("Chưa Chọn Địa Chỉ", "Vui lòng chọn địa chỉ giao hàng để tiếp tục đặt hàng", "warning");
      return;
    }

    // Kiểm tra phương thức thanh toán
    const selectedPayment = document.querySelector('input[name="payment"]:checked');
    if (!selectedPayment) {
      showAlertModal("Chưa Chọn Phương Thức", "Vui lòng chọn phương thức thanh toán để hoàn tất đơn hàng", "warning");
      return;
    }

    // // Kiểm tra nếu chọn thanh toán MoMo
    // const paymentMethods = document.querySelectorAll('input[name="payment"]');
    // const isMomoPayment = paymentMethods[0].checked; // Phương thức đầu tiên là MoMo

    // if (isMomoPayment) {
    //   // Hiển thị modal QR Code MoMo
    //   const momoQrModal = document.getElementById('momoQrModal');
    //   momoQrModal.classList.remove('d-none');
    //   momoQrModal.classList.add('d-flex');
    //   return;
    // }

    // Nếu thanh toán COD, xử lý ngay
    processOrder();
  });
}

// Hàm xử lý đơn hàng
// Hàm xử lý đơn hàng
async function processOrder() {
  try {
    // Lấy thông tin cần thiết
    const selectedAddress = document.querySelector('input[name="address"]:checked');
    const selectedPayment = document.querySelector('input[name="payment"]:checked');
    const orderItems = getValidOrderItems(); // Sử dụng hàm getValidOrderItems thay vì lấy trực tiếp từ localStorage
    
    console.log('ProcessOrder - isBuyNow:', window.isBuyNow);
    console.log('ProcessOrder - orderItems:', orderItems);
    
    if (!selectedAddress) {
      showAlertModal("Lỗi", "Vui lòng chọn địa chỉ giao hàng", "error");
      return;
    }
    
    if (!selectedPayment) {
      showAlertModal("Lỗi", "Vui lòng chọn phương thức thanh toán", "error");
      return;
    }
    
    if (orderItems.length === 0) {
      const errorMessage = window.isBuyNow ? "Không thể xử lý sản phẩm mua ngay" : "Không có sản phẩm nào để đặt hàng";
      showAlertModal("Lỗi", errorMessage, "error");
      return;
    }

    // Hiển thị thông báo đang xử lý
    showAlertModal("Đang Xử Lý", "Đơn hàng của bạn đang được xử lý...", "fas fa-spinner", true);

    // Chuẩn bị dữ liệu đơn hàng
    const paymentMethod = document.querySelector('input[name="payment"]:checked').value;
    
    // Tính tổng tiền
    const subtotal = orderItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    const shippingFee = 30000;
    const totalAmount = subtotal + shippingFee;

    // Chuyển đổi orderItems thành format phù hợp cho server
    const orderItemsForServer = orderItems.map(item => ({
      idBienThe: item.idBienThe || 0, // Sử dụng idBienThe thực tế từ localStorage
      soLuong: item.quantity,
      donGia: item.price
    }));

    console.log('Order items for server:', orderItemsForServer);

    // Gửi request đến server
    const response = await fetch('/KhachHang/Pay/ProcessOrder', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Requested-With': 'XMLHttpRequest'
      },
      body: JSON.stringify({
        addressId: parseInt(selectedAddress.value),
        paymentMethod: paymentMethod,
        totalAmount: totalAmount,
        note: '',
        orderItems: orderItemsForServer
      })
    });

    const result = await response.json();
    
if (result.success) {
        
        // === BẮT ĐẦU LOGIC PHÂN NHÁNH VNPAY / COD ===

        if (result.paymentUrl) {
            // TRƯỜNG HỢP 1: VNPAY
            // Server trả về URL, chúng ta chuyển hướng
            console.log('Đang chuyển hướng đến VNPAY:', result.paymentUrl);
            window.location.href = result.paymentUrl;
        
        } else {
            // TRƯỜNG HỢP 2: COD
            // Chuyển hướng về trang thông báo thành công với mã đơn hàng
            console.log('Đặt hàng COD thành công, đang chuyển hướng...');
            
            // Chỉ xóa localStorage khi không phải chế độ "mua ngay"
            if (!window.isBuyNow) {
                localStorage.removeItem('orderItems');
                localStorage.removeItem('cartSelected');
                localStorage.removeItem('selectedCartItems');
                console.log('Đã xóa dữ liệu localStorage (chế độ giỏ hàng)');
            } else {
                console.log('Bỏ qua xóa localStorage (chế độ mua ngay)');
            }
            
            // Chuyển hướng đến trang thành công với orderCode
            window.location.href = `/KhachHang/DonHang/DatHangThanhCong?orderCode=${result.orderCode}`;
        }
        // === KẾT THÚC LOGIC PHÂN NHÁNH ===

    } else {
        closeAlertModal();
        showAlertModal("Lỗi", result.message || "Có lỗi xảy ra khi đặt hàng", "error");
    }
  } catch (error) {
    console.error('Lỗi khi xử lý đơn hàng:', error);
    closeAlertModal();
    showAlertModal("Lỗi", "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.", "error");
  }
}

// Xử lý đóng modal thành công (backup - gắn sự kiện từ đầu)
document.addEventListener('DOMContentLoaded', function() {
  console.log('DOM đã load, backup event cho nút X...');
  
  // Backup method - gắn sự kiện toàn cục
  document.addEventListener('click', function(e) {
    if (e.target && (e.target.id === 'closeSuccessModalBtn' || e.target.closest('#closeSuccessModalBtn'))) {
      console.log('Global click: Nút X được click!');
      e.preventDefault();
      e.stopPropagation();
      closeSuccessModal();
    }
    
    // Xử lý nút "Xác nhận hủy" bằng event delegation
    if (e.target && e.target.id === 'alertModalOk' && e.target.innerHTML.includes('Xác nhận hủy')) {
      console.log('Global click: Nút Xác nhận hủy được click!');
      e.preventDefault();
      e.stopPropagation();
      
      // Đóng alert modal
      const alertModal = document.getElementById('alertModal');
      if (alertModal) {
        alertModal.classList.remove('d-flex');
        alertModal.classList.add('d-none');
      }
      
      // Đóng modal MoMo
      const momoQrModal = document.getElementById('momoQrModal');
      if (momoQrModal) {
        momoQrModal.classList.remove('d-flex');
        momoQrModal.classList.add('d-none');
        console.log('Modal MoMo đã được đóng bằng event delegation');
      }
      
      // Reset nút OK
      setTimeout(() => {
        if (e.target) {
          e.target.innerHTML = '<i class="fas fa-check"></i> Đã hiểu';
          e.target.onclick = closeAlertModal;
        }
      }, 100);
    }
  });
  
  // Thêm sự kiện click vào overlay để đóng modal
  const successModal = document.getElementById('paymentSuccessModal');
  if (successModal) {
    successModal.addEventListener('click', function(e) {
      // Chỉ đóng khi click vào overlay (không phải modal-content)
      if (e.target === successModal) {
        console.log('Click vào overlay, đóng modal...');
        closeSuccessModal();
      }
    });
  }
});

// Xử lý đóng modal không có địa chỉ
const closeNoAddressModalBtn = document.getElementById('closeNoAddressModalBtn');
const closeNoAddressBtn = document.getElementById('closeNoAddressBtn');
const addAddressFromModalBtn = document.getElementById('addAddressFromModalBtn');
const noAddressModal = document.getElementById('noAddressModal');

[closeNoAddressModalBtn, closeNoAddressBtn].forEach(btn => {
  btn?.addEventListener('click', () => {
    noAddressModal.classList.remove('d-flex');
    noAddressModal.classList.add('d-none');
  });
});

// Xử lý nút thêm địa chỉ từ modal thông báo
addAddressFromModalBtn?.addEventListener('click', () => {
  // Đóng modal thông báo
  noAddressModal.classList.remove('d-flex');
  noAddressModal.classList.add('d-none');
  
  // Mở modal thêm địa chỉ
  addressModal.classList.remove('d-none');
  addressModal.classList.add('d-flex');
});

// Thêm hiệu ứng hover cho nút quay lại
const backBtnHover = document.getElementById('backToCartBtn');
if (backBtnHover) {
  backBtnHover.addEventListener('mouseenter', () => {
    backBtnHover.style.background = 'var(--lighter)';
    backBtnHover.style.borderColor = 'var(--primary)';
    backBtnHover.style.transform = 'translateY(-2px)';
  });

  backBtnHover.addEventListener('mouseleave', () => {
    backBtnHover.style.background = 'var(--white)';
    backBtnHover.style.borderColor = 'var(--border)';
    backBtnHover.style.transform = 'translateY(0)';
  });
}

function formatVND(n) {
  return n.toLocaleString('vi-VN') + '₫';
}

// Kiểm tra dữ liệu từ localStorage và xác thực
function getValidOrderItems() {
  try {
    // Kiểm tra nếu là chế độ "mua ngay"
    if (window.isBuyNow && window.buyNowData) {
      console.log('Processing Buy Now item');
      // Chuyển đổi dữ liệu "mua ngay" sang format phù hợp
      const buyNowItem = {
        name: window.buyNowData.name,
        price: window.buyNowData.price,
        quantity: window.buyNowData.quantity,
        total: window.buyNowData.total,
        idBienThe: window.buyNowData.idBienThe,
        linkAnh: window.buyNowData.linkAnh || '/images/noimage.jpg'
      };
      
      console.log('[Pay Debug] Buy Now Item:', buyNowItem);
      
      // Validate dữ liệu
      if (buyNowItem.name && 
          buyNowItem.price > 0 && 
          buyNowItem.quantity > 0 &&
          buyNowItem.idBienThe > 0) {
        return [buyNowItem];
      } else {
        console.error('Invalid Buy Now data:', buyNowItem);
        return [];
      }
    }
    
    // Nếu không phải "mua ngay", lấy từ localStorage như bình thường
    const orderItems = JSON.parse(localStorage.getItem('orderItems')) || [];
    
    console.log('[Pay Debug] Order Items from localStorage:', orderItems);
    
    // Kiểm tra xem localStorage có dữ liệu hợp lệ không
    if (!Array.isArray(orderItems) || orderItems.length === 0) {
      return [];
    }
    
    // Validate từng item
    const validItems = orderItems.filter(item => {
      return item && 
             typeof item.name === 'string' && 
             typeof item.price === 'number' && 
             typeof item.quantity === 'number' &&
             typeof item.idBienThe === 'number' &&
             item.price > 0 && 
             item.quantity > 0 &&
             item.idBienThe > 0;
    });
    
    // Đảm bảo mỗi item có linkAnh (nếu không có thì dùng default)
    const processedItems = validItems.map(item => ({
      ...item,
      linkAnh: item.linkAnh || '/images/noimage.jpg'
    }));
    
    console.log('[Pay Debug] Processed Items:', processedItems);
    
    return processedItems;
  } catch (error) {
    console.error('Error parsing orderItems:', error);
    return [];
  }
}

// Hàm khởi tạo hiển thị sản phẩm
function initializeOrderDisplay() {
  const container = document.getElementById('orderItemsContainer');
  if (!container) return;
  
  const orderItems = getValidOrderItems();

  if (orderItems.length === 0) {
    const isBuyNowMode = window.isBuyNow || false;
    const backButtonText = isBuyNowMode ? 'Quay lại sản phẩm' : 'Quay lại giỏ hàng';
    const backButtonLink = isBuyNowMode ? 'javascript:history.back()' : '/KhachHang/Cart';
    
    container.innerHTML = `
      <div class="text-center py-5">
        <i class="fas fa-shopping-cart mb-3" style="font-size: 3rem; color: #2E7D32; opacity: 0.5;"></i>
        <p class="text-muted fs-5">Không có sản phẩm nào trong đơn hàng.</p>
        <p class="text-muted">${isBuyNowMode ? 'Vui lòng thử lại.' : 'Vui lòng quay lại giỏ hàng để chọn sản phẩm.'}</p>
        <button onclick="window.location.href='${backButtonLink}'" class="btn btn-primary mt-2">
          <i class="fas fa-arrow-left me-2"></i>${backButtonText}
        </button>
      </div>`;
      
    // Ẩn nút hoàn tất đơn hàng khi không có sản phẩm
    const completeBtn = document.getElementById('completeOrderBtn');
    if (completeBtn) {
      completeBtn.style.display = 'none';
    }
    
    // Cập nhật tổng tiền về 0
    const subtotalElement = document.getElementById('subtotalAmount');
    const totalElement = document.getElementById('totalAmount');
    if (subtotalElement) subtotalElement.textContent = formatVND(0);
    if (totalElement) totalElement.textContent = formatVND(0); // Miễn phí vận chuyển
  } else {
    let subtotal = 0;
    container.innerHTML = orderItems.map(item => {
      const itemTotal = item.price * item.quantity;
      subtotal += itemTotal;
    return `
      <div class="product-item d-flex align-items-center p-3 mb-2 rounded-3">
        <div class="product-image-wrapper me-3">
          <img src="${item.linkAnh || '/images/noimage.jpg'}" alt="Product" class="img-fluid" />
        </div>
        <div class="flex-grow-1">
          <h6 class="fw-semibold mb-1" style="color: var(--primary); font-size: 1rem;">${item.name}</h6>
          <div class="d-flex justify-content-between align-items-center">
            <div class="d-flex align-items-center text-muted">
              <i class="fas fa-cube me-1" style="color: var(--primary); font-size: 0.8rem;"></i>
              <small>${item.quantity} x ${formatVND(item.price)}</small>
            </div>
            <div class="fw-bold text-end" style="color: var(--primary); font-size: 1.1rem;">${formatVND(itemTotal)}</div>
          </div>
        </div>
      </div>`;
    }).join('');

    // Cập nhật tổng tiền (không cộng phí vận chuyển)
    const subtotalElement = document.getElementById('subtotalAmount');
    const totalElement = document.getElementById('totalAmount');
    if (subtotalElement) subtotalElement.textContent = formatVND(subtotal);
    if (totalElement) totalElement.textContent = formatVND(subtotal); // Miễn phí vận chuyển
    
    // Hiển thị nút hoàn tất đơn hàng
    const completeBtn = document.getElementById('completeOrderBtn');
    if (completeBtn) {
      completeBtn.style.display = 'block';
    }
  }
}

// Event đã được xử lý ở trên, comment lại để tránh duplicate
// const backToCartBtnDup = document.getElementById("backToCartBtn");
// if (backToCartBtnDup) {
//     backToCartBtnDup.addEventListener("click", function () {
//         if (window.isBuyNow) {
//             history.back();
//         } else {
//             window.location.href = "/KhachHang/Cart";
//         }
//     });
// }

// Alert Modal System - Single unified implementation
function showAlertModal(title, message, icon = 'fas fa-exclamation-circle', autoClose = false) {
    document.getElementById('alertModalTitle').textContent = title;
    document.getElementById('alertModalMessage').textContent = message;
    document.getElementById('alertModalIcon').className = icon;

    const footer = document.querySelector('#alertModal .modal-footer');
    const closeBtn = document.getElementById('alertModalClose');

    if (autoClose) {
        footer.style.display = 'none';      // Ẩn footer
        closeBtn.style.display = 'none';    // Ẩn nút X
        // Đặt biến trạng thái để kiểm tra đã đóng chưa
        const alertModal = document.getElementById('alertModal');
        alertModal._autoCloseTimeout = setTimeout(() => {
            // Chỉ đóng nếu modal vẫn đang mở
            if (alertModal.classList.contains('d-flex')) {
                closeAlertModal();
            }
        }, 2000);
    } else {
        footer.style.display = 'flex';      // Hiện footer
        closeBtn.style.display = '';        // Hiện nút X
    }

    document.getElementById('alertModal').classList.remove('d-none');
    document.getElementById('alertModal').classList.add('d-flex');
    
    // Thêm sự kiện click overlay để đóng modal
    const alertModal = document.getElementById('alertModal');
    function outsideClickHandler(e) {
        // Chỉ đóng khi click vào chính alertModal (overlay), không phải modal-content
        if (e.target === alertModal) {
            closeAlertModal();
        }
    }
    alertModal.addEventListener('mousedown', outsideClickHandler);
    alertModal._outsideClickHandler = outsideClickHandler;
}

function closeAlertModal() {
    const alertModal = document.getElementById('alertModal');
    alertModal.classList.remove('d-flex');
    alertModal.classList.add('d-none');
    
    // Chỉ reset nút OK nếu không phải đang trong quá trình xử lý đặc biệt
    const alertModalOk = document.getElementById('alertModalOk');
    const isProcessingSpecialAction = alertModalOk && alertModalOk.innerHTML.includes('Xác nhận hủy');
    
    if (!isProcessingSpecialAction) {
        // Reset nút OK về trạng thái ban đầu (chỉ khi không đang xử lý action đặc biệt)
        alertModalOk.innerHTML = '<i class="fas fa-check"></i> Đã hiểu';
        alertModalOk.onclick = closeAlertModal;
    }
    
    // Xóa sự kiện click overlay khi đóng
    if (alertModal._outsideClickHandler) {
        alertModal.removeEventListener('mousedown', alertModal._outsideClickHandler);
        delete alertModal._outsideClickHandler;
    }
    // Nếu có timeout autoClose thì clear luôn
    if (alertModal._autoCloseTimeout) {
        clearTimeout(alertModal._autoCloseTimeout);
        delete alertModal._autoCloseTimeout;
    }
}

// Event listeners for alert modal
const alertModalClose = document.getElementById('alertModalClose');
const alertModalOk = document.getElementById('alertModalOk');
if (alertModalClose) {
  alertModalClose.addEventListener('click', closeAlertModal);
}
if (alertModalOk) {
  alertModalOk.addEventListener('click', closeAlertModal);
}

// Xử lý modal MoMo QR Code
document.addEventListener('DOMContentLoaded', function() {
  // Xử lý đóng modal MoMo QR
  const cancelMomoPaymentBtn = document.getElementById('cancelMomoPaymentBtn');
  const confirmMomoPaymentBtn = document.getElementById('confirmMomoPaymentBtn');
  const momoQrModal = document.getElementById('momoQrModal');

  // Hàm để đóng modal MoMo
  function closeMomoModal() {
    if (momoQrModal) {
      momoQrModal.classList.remove('d-flex');
      momoQrModal.classList.add('d-none');
      console.log('Modal MoMo đã được đóng');
    }
  }

  // Hàm xử lý xác nhận hủy thanh toán MoMo
  function confirmCancelMomoPayment() {
    console.log('Đang thực hiện hủy thanh toán MoMo...');
    
    // Đóng alert modal trước
    closeAlertModal();
    
    // Đóng modal MoMo với một chút delay để mượt mà
    setTimeout(() => {
      closeMomoModal();
      console.log('Đã hủy thanh toán MoMo và quay về trang thanh toán');
    }, 200);
  }

  // Đóng modal khi click nút Hủy
  cancelMomoPaymentBtn?.addEventListener('click', () => {
    // Hiện modal xác nhận hủy
    showAlertModal(
      "Xác nhận hủy thanh toán", 
      "Bạn có chắc chắn muốn hủy thanh toán MoMo và quay lại trang thanh toán?", 
      "fas fa-question-circle"
    );
    
    // Thay đổi nút OK thành nút xác nhận hủy và thêm logic đóng modal MoMo
    setTimeout(() => {
      const alertModalOk = document.getElementById('alertModalOk');
      if (alertModalOk) {
        // Lưu trạng thái ban đầu
        const originalText = alertModalOk.innerHTML;
        const originalOnClick = alertModalOk.onclick;
        
        // Thay đổi nút thành "Xác nhận hủy"
        alertModalOk.innerHTML = '<i class="fas fa-check"></i> Xác nhận hủy';
        
        // Xóa event listener cũ và thêm mới
        alertModalOk.onclick = null;
        alertModalOk.removeEventListener('click', closeAlertModal);
        
        // Thêm event listener mới cho "Xác nhận hủy"
        alertModalOk.onclick = confirmCancelMomoPayment;
        
        // Thêm fallback event listener
        alertModalOk.addEventListener('click', function(e) {
          e.preventDefault();
          e.stopPropagation();
          confirmCancelMomoPayment();
        });
        
        // Reset nút về trạng thái ban đầu sau 1 phút (cleanup)
        setTimeout(() => {
          if (alertModalOk) {
            alertModalOk.innerHTML = originalText || '<i class="fas fa-check"></i> Đã hiểu';
            alertModalOk.onclick = originalOnClick || closeAlertModal;
          }
        }, 60000);
      }
    }, 100);
  });

  // Xác nhận thanh toán MoMo
  confirmMomoPaymentBtn?.addEventListener('click', () => {
    // Đóng modal MoMo QR
    closeMomoModal();
    
    // Xử lý đơn hàng
    processOrder();
  });
});

/**
 * Cài đặt sự kiện khi người dùng thay đổi phương thức thanh toán (COD/VNPAY/STRIPE)
 */
function setupPaymentMethodToggle() {
    const paymentRadios = document.querySelectorAll('input[name="payment"]');
    if (!paymentRadios || paymentRadios.length === 0) {
        console.warn('[Payment] No payment method radios found');
        return;
    }
    
    paymentRadios.forEach(radio => {
        radio.addEventListener('change', handlePaymentMethodChange);
    });

    // Gọi 1 lần ban đầu để cài đặt trạng thái đúng (hiển thị nút COD/VNPAY)
    handlePaymentMethodChange();
    console.log('[Payment] Payment method toggle initialized');
}

/**
 * Xử lý ẩn/hiện các nút bấm và div thanh toán
 */
function handlePaymentMethodChange() {
    const selectedPayment = document.querySelector('input[name="payment"]:checked');
    if (!selectedPayment) {
        console.log('[Payment] No payment method selected');
        return;
    }

    const paymentMethod = selectedPayment.value;
    const codVnpaySubmitBtn = document.getElementById('completeOrderBtn'); // Nút "Hoàn Tất" cũ
    const stripeContainer = document.getElementById('stripe-payment-element-container'); // Div thẻ mới

    if (!codVnpaySubmitBtn || !stripeContainer) {
        console.warn('[Payment] Required elements not found:', {
            codVnpaySubmitBtn: !!codVnpaySubmitBtn,
            stripeContainer: !!stripeContainer
        });
        return;
    }

    console.log('[Payment] Selected payment method:', paymentMethod);

    if (paymentMethod === 'STRIPE') {
        // --- CHỌN STRIPE ---
        codVnpaySubmitBtn.style.display = 'none'; // Ẩn nút "Hoàn Tất Đơn Hàng" cũ
        stripeContainer.style.display = 'block'; // Hiện khối nhập thẻ Stripe

        // Luôn gọi initializeStripeElements() để xử lý logic đầy đủ
        console.log('[Payment] Initializing Stripe Elements...');
        initializeStripeElements();
    } else {
        // --- CHỌN COD hoặc VNPAY ---
        codVnpaySubmitBtn.style.display = 'block'; // Hiện nút "Hoàn Tất Đơn Hàng" cũ
        stripeContainer.style.display = 'none'; // Ẩn khối nhập thẻ Stripe
    }
}

/**
 * Mount Stripe Payment Element vào container
 */
async function mountStripePaymentElement(targetContainer) {
    if (!targetContainer) {
        console.error('[Stripe Mount] Target container not provided');
        return;
    }
    
    if (!currentStripeClientSecret) {
        console.error('[Stripe Mount] No client secret available');
        targetContainer.innerHTML = '<p style="color: red;">Lỗi: Chưa khởi tạo thanh toán</p>';
        return;
    }
    
    console.log('[Stripe Mount] Mounting payment element...');
    targetContainer.innerHTML = ''; // Clear spinner
    
    const appearance = { 
        theme: 'stripe',
        variables: {
            colorPrimary: '#2d7b2c'
        }
    };
    
    elements = stripe.elements({ 
        appearance, 
        clientSecret: currentStripeClientSecret,
    });
    
    const paymentElementOptions = { 
        layout: 'accordion',
    };

    const paymentElement = elements.create("payment", paymentElementOptions);
    paymentElement.mount(targetContainer);
    
    console.log('[Stripe] Payment Element mounted for new card');
}

/**
 * Hiển thị saved payment methods cho user chọn
 */
function displaySavedPaymentMethods(paymentMethods) {
    const container = document.getElementById('payment-element');
    
    let html = '<div style="margin-bottom: 20px;">';
    html += '<h4 style="color: var(--primary); margin-bottom: 15px;"><i class="fas fa-credit-card"></i> Thẻ đã lưu</h4>';
    
    paymentMethods.forEach((pm, index) => {
        const brandIcon = pm.brand === 'visa' ? 'fab fa-cc-visa' : 
                         pm.brand === 'mastercard' ? 'fab fa-cc-mastercard' :
                         pm.brand === 'amex' ? 'fab fa-cc-amex' : 'fas fa-credit-card';
        
        html += `
            <label class="saved-card-option" style="display: flex; align-items: center; padding: 12px; border: 2px solid #e0e0e0; border-radius: 8px; margin-bottom: 10px; cursor: pointer; transition: all 0.3s;">
                <input type="radio" name="savedCard" value="${pm.id}" ${index === 0 ? 'checked' : ''} style="margin-right: 12px; transform: scale(1.2);">
                <i class="${brandIcon}" style="font-size: 24px; margin-right: 12px; color: var(--primary);"></i>
                <div>
                    <div style="font-weight: 600;">${pm.brand.toUpperCase()} •••• ${pm.last4}</div>
                    <div style="font-size: 12px; color: #666;">Hết hạn: ${pm.expMonth}/${pm.expYear}</div>
                </div>
            </label>
        `;
    });
    
    html += `
        <label class="saved-card-option" style="display: flex; align-items: center; padding: 12px; border: 2px solid #e0e0e0; border-radius: 8px; margin-bottom: 10px; cursor: pointer;">
            <input type="radio" name="savedCard" value="new" style="margin-right: 12px; transform: scale(1.2);">
            <i class="fas fa-plus-circle" style="font-size: 24px; margin-right: 12px; color: var(--primary);"></i>
            <div style="font-weight: 600;">Sử dụng thẻ mới</div>
        </label>
    </div>
    <div id="new-card-element" style="display: none;"></div>
    `;
    
    container.innerHTML = html;
    
    // Add event listeners
    document.querySelectorAll('input[name="savedCard"]').forEach(radio => {
        radio.addEventListener('change', async (e) => {
            const newCardContainer = document.getElementById('new-card-element');
            if (e.target.value === 'new') {
                console.log('[Stripe] User selected "Use new card", mounting Payment Element...');
                newCardContainer.style.display = 'block';
                newCardContainer.innerHTML = '<div class="text-center p-3"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>';
                
                // Mount Stripe Payment Element khi chọn thẻ mới
                await mountStripePaymentElement(newCardContainer);
            } else {
                console.log('[Stripe] User selected saved card:', e.target.value);
                newCardContainer.style.display = 'none';
                newCardContainer.innerHTML = '';
            }
        });
    });
    
    // Add hover effect
    document.querySelectorAll('.saved-card-option').forEach(label => {
        label.addEventListener('mouseenter', () => {
            label.style.borderColor = 'var(--primary)';
            label.style.backgroundColor = '#f8fff8';
        });
        label.addEventListener('mouseleave', () => {
            const radio = label.querySelector('input[type="radio"]');
            if (!radio.checked) {
                label.style.borderColor = '#e0e0e0';
                label.style.backgroundColor = 'white';
            }
        });
        
        const radio = label.querySelector('input[type="radio"]');
        radio.addEventListener('change', () => {
            document.querySelectorAll('.saved-card-option').forEach(l => {
                l.style.borderColor = '#e0e0e0';
                l.style.backgroundColor = 'white';
            });
            if (radio.checked) {
                label.style.borderColor = 'var(--primary)';
                label.style.backgroundColor = '#f8fff8';
            }
        });
    });
}

/**
 * Gọi API server (StripeController) để tạo PaymentIntent (Ý định thanh toán)
 * và lấy về clientSecret.
 */
async function initializeStripeElements() {
    console.log('[Stripe Init] Starting initialization...');
    
    // Kiểm tra nếu đã khởi tạo xong rồi thì không làm gì
    if (currentStripeClientSecret && elements) {
        console.log('[Stripe Init] Already initialized, skipping...');
        return;
    }
    
    // 1. Kiểm tra địa chỉ
    const selectedAddress = document.querySelector('input[name="address"]:checked');
    if (!selectedAddress) {
        console.warn('[Stripe Init] No address selected');
        showAlertModal("Lỗi", "Vui lòng chọn địa chỉ giao hàng trước khi chọn phương thức thanh toán Stripe.", "fas fa-exclamation-triangle");
        // Tự động chuyển về COD
        const codRadio = document.getElementById('payment_cod');
        if (codRadio) {
            codRadio.checked = true;
            handlePaymentMethodChange(); // Gọi lại để ẩn Stripe
        }
        return;
    }
    const addressId = parseInt(selectedAddress.value);
    console.log('[Stripe Init] Address ID:', addressId);

    // 1.5. Kiểm tra xem có saved payment methods không
    let hasSavedPaymentMethods = false;
    try {
        const savedPMResponse = await fetch('/KhachHang/Stripe/GetSavedPaymentMethods');
        const savedPMResult = await savedPMResponse.json();
        
        if (savedPMResult.success && savedPMResult.paymentMethods && savedPMResult.paymentMethods.length > 0) {
            console.log('[Stripe] Found saved payment methods:', savedPMResult.paymentMethods.length);
            hasSavedPaymentMethods = true;
            displaySavedPaymentMethods(savedPMResult.paymentMethods);
            // DỪNG Ở ĐÂY - không cần mount Payment Element ngay
            // Payment Element sẽ được mount khi user chọn "Sử dụng thẻ mới"
        } else {
            console.log('[Stripe] No saved payment methods found');
        }
    } catch (error) {
        console.log('[Stripe] Could not fetch saved payment methods:', error);
    }

    // NẾU ĐÃ CÓ SAVED CARDS, chỉ cần tạo PaymentIntent và return
    if (hasSavedPaymentMethods) {
        // Vẫn cần tạo PaymentIntent để có clientSecret cho việc thanh toán
        try {
            const orderItems = getValidOrderItems();
            const requestData = { 
                addressId: addressId,
                orderItems: orderItems.map(item => ({
                    idBienThe: item.idBienThe,
                    soLuong: item.quantity,
                    donGia: item.price
                }))
            };
            
            const response = await fetch('/KhachHang/Stripe/CreatePaymentIntent', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();
            
            if (result.success) {
                currentStripeClientSecret = result.clientSecret;
                currentStripePaymentIntentId = result.paymentIntentId; // Lưu PI ID thay vì orderId
                console.log('[Stripe] PaymentIntent created for saved card payment');
            }
        } catch (error) {
            console.error('[Stripe] Error creating PaymentIntent:', error);
        }
        return; // DỪNG - không mount Payment Element
    }

    // CHỈ CHẠY CODE DƯỚI ĐÂY NẾU KHÔNG CÓ SAVED CARDS
    // Hiển thị loading (thay thế #payment-element bằng spinner)
    const paymentElementDiv = document.getElementById('payment-element');
    if (!paymentElementDiv) {
        console.error('[Stripe Init] Payment element div not found');
        return;
    }
    
    paymentElementDiv.innerHTML =
        '<div class="text-center p-3"><i class="fas fa-spinner fa-spin" style="font-size: 1.5rem; color: var(--primary);"></i> Đang tải biểu mẫu thẻ...</div>';
    
    const paymentMessageDiv = document.getElementById('payment-message');
    if (paymentMessageDiv) {
        paymentMessageDiv.textContent = ""; // Xóa lỗi cũ
    }

    try {
        // 2. Gọi API CreatePaymentIntent
        // Lấy orderItems để gửi (hỗ trợ cả mua ngay và giỏ hàng)
        const orderItems = getValidOrderItems();
        const requestData = { 
            addressId: addressId,
            orderItems: orderItems.map(item => ({
                idBienThe: item.idBienThe,
                soLuong: item.quantity,
                donGia: item.price
            }))
        };
        
        console.log('[Stripe Init] Request data:', requestData);
        
        const response = await fetch('/KhachHang/Stripe/CreatePaymentIntent', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });

        const result = await response.json();

        if (result.success) {
            // 3. Lấy clientSecret và OrderId
            currentStripeClientSecret = result.clientSecret;
            currentStripeOrderId = result.orderId; // Lưu ID đơn hàng (ví dụ: 29)
            
            console.log('[Stripe] ClientSecret received:', currentStripeClientSecret);
            console.log('[Stripe] Order ID:', currentStripeOrderId);
            
            // 4. QUAN TRỌNG: Clear toàn bộ nội dung div trước khi mount
            const paymentElementDiv = document.getElementById('payment-element');
            if (!paymentElementDiv) {
                console.error('[Stripe Init] Payment element div not found');
                return;
            }
            paymentElementDiv.innerHTML = ''; // Xóa spinner và mọi child nodes
            
            console.log('[Stripe Init] Payment element cleared, ready to mount');
            
            // 5. Khởi tạo Stripe Elements với clientSecret (bao gồm customer info)
            const appearance = { 
                theme: 'stripe',
                variables: {
                    colorPrimary: '#2d7b2c'
                }
            };
            
            // Cấu hình Elements - clientSecret đã chứa Customer ID
            elements = stripe.elements({ 
                appearance, 
                clientSecret: currentStripeClientSecret,
            });
            
            console.log('[Stripe] Elements initialized. ClientSecret contains customer data.');
            
            // Cấu hình Payment Element
            const paymentElementOptions = { 
                layout: 'accordion', // Thử dùng accordion thay vì tabs để show saved cards rõ hơn
                defaultValues: {
                    billingDetails: {
                        // Có thể thêm thông tin billing nếu cần
                    }
                },
                fields: {
                    billingDetails: 'auto' // Tự động điền billing details
                },
            };

            const paymentElement = elements.create("payment", paymentElementOptions);
            
            console.log('[Stripe] Payment Element created with accordion layout');
            
            // Lắng nghe sự kiện ready để debug
            paymentElement.on('ready', () => {
                console.log('[Stripe] Payment Element is ready and rendered');
                console.log('[Stripe] Check if saved payment methods are visible in the UI above');
            });
            
            paymentElement.on('change', (event) => {
                console.log('[Stripe] Payment Element changed:', event);
                if (event.complete) {
                    console.log('[Stripe] Payment method selected and complete');
                }
                if (event.value?.type) {
                    console.log('[Stripe] Selected payment type:', event.value.type);
                }
            });
            
            paymentElement.mount("#payment-element"); // Gắn vào div đã clear
            
            console.log('[Stripe Init] Payment Element mounted successfully');
        } else {
            console.error('[Stripe Init] Failed to create PaymentIntent:', result.message);
            const paymentElementDiv = document.getElementById('payment-element');
            const paymentMessageDiv = document.getElementById('payment-message');
            if (paymentElementDiv) {
                paymentElementDiv.innerHTML = ''; // Xóa spinner
            }
            if (paymentMessageDiv) {
                paymentMessageDiv.textContent = result.message || "Lỗi khi khởi tạo Stripe. Vui lòng thử lại.";
            }
        }
    } catch (error) {
        console.error('[Stripe Init] Exception occurred:', error);
        const paymentElementDiv = document.getElementById('payment-element');
        const paymentMessageDiv = document.getElementById('payment-message');
        if (paymentElementDiv) {
            paymentElementDiv.innerHTML = ''; // Xóa spinner
        }
        if (paymentMessageDiv) {
            paymentMessageDiv.textContent = "Lỗi kết nối máy chủ. Vui lòng tải lại trang.";
        }
    }
}

/**
 * Xử lý khi người dùng nhấn nút "Thanh toán Thẻ" (nút của Stripe)
 */
async function handleStripeSubmit(e) {
    e.preventDefault();
    
    console.log('[Stripe Submit] Starting payment process...');
    
    if (!currentStripeClientSecret) {
        console.error('[Stripe Submit] No client secret found');
        document.getElementById('payment-message').textContent = "Lỗi khởi tạo. Vui lòng chọn lại phương thức thanh toán.";
        return;
    }

    const submitBtn = document.getElementById('stripe-submit-button');
    if (!submitBtn) {
        console.error('[Stripe Submit] Submit button not found');
        return;
    }
    
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin" style="margin-right: 0.75rem;"></i> Đang xử lý...';

    try {
        // Kiểm tra xem user chọn saved card hay new card
        const selectedCardRadio = document.querySelector('input[name="savedCard"]:checked');
        
        if (selectedCardRadio && selectedCardRadio.value !== 'new') {
            // User chọn saved card
            const paymentMethodId = selectedCardRadio.value;
            console.log('[Stripe Submit] Using saved payment method:', paymentMethodId);
            
            // Confirm payment với saved payment method
            const { error } = await stripe.confirmCardPayment(currentStripeClientSecret, {
                payment_method: paymentMethodId,
            });
            
            if (error) {
                console.error('[Stripe Submit] Payment failed:', error);
                document.getElementById('payment-message').textContent = error.message;
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-check-circle" style="margin-right: 0.75rem;"></i><span>Thanh toán Thẻ</span>';
            } else {
                // Thanh toán thành công - chuyển đến trang success
                console.log('[Stripe Submit] Payment succeeded with saved card');
                // Redirect đến action xử lý return với payment_intent
                window.location.href = `/KhachHang/DonHang/StripePaymentReturn?payment_intent=${currentStripePaymentIntentId}`;
            }
        } else {
            // User chọn nhập thẻ mới - dùng Payment Element
            console.log('[Stripe Submit] Using new card with Payment Element');
            
            if (!stripe || !elements) {
                console.error('[Stripe Submit] Stripe or Elements not initialized');
                document.getElementById('payment-message').textContent = "Vui lòng nhập thông tin thẻ mới.";
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-check-circle" style="margin-right: 0.75rem;"></i><span>Thanh toán Thẻ</span>';
                return;
            }
            
            const { error } = await stripe.confirmPayment({
                elements,
                confirmParams: {
                    return_url: `${window.location.origin}/KhachHang/DonHang/StripePaymentReturn`, // Chuyển đến action xử lý return
                },
            });

            if (error) {
                console.error('[Stripe Submit] Payment confirmation failed:', error);
                if (error.type === "card_error" || error.type === "validation_error") {
                    document.getElementById('payment-message').textContent = error.message;
                } else {
                    document.getElementById('payment-message').textContent = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.";
                }
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-check-circle" style="margin-right: 0.75rem;"></i><span>Thanh toán Thẻ</span>';
            } else {
                console.log('[Stripe Submit] Payment succeeded, redirecting...');
            }
        }
    } catch (err) {
        console.error('[Stripe Submit] Unexpected error:', err);
        document.getElementById('payment-message').textContent = "Đã xảy ra lỗi. Vui lòng thử lại.";
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i class="fas fa-check-circle" style="margin-right: 0.75rem;"></i><span>Thanh toán Thẻ</span>';
    }
}