// ==================== GLOBAL VARIABLES ====================
// Các biến này PHẢI được định nghĩa từ inline script TRƯỚC KHI load file này
// File này CHỈ kiểm tra, KHÔNG khởi tạo lại để tránh ghi đè dữ liệu từ server
if (typeof window.maxQuantity === "undefined") window.maxQuantity = 0;
if (typeof window.productId === "undefined") window.productId = 0;
if (typeof window.productVariants === "undefined") window.productVariants = [];
if (typeof window.selectedAttributes === "undefined")
  window.selectedAttributes = {};
if (typeof window.selectedVariantId === "undefined")
  window.selectedVariantId = null;
if (typeof window.selectedVariantPrice === "undefined")
  window.selectedVariantPrice = null;
if (typeof window.currentSlide === "undefined") window.currentSlide = 0;

console.log("[ChiTiet] JavaScript loaded successfully");
console.log("[ChiTiet] maxQuantity:", window.maxQuantity);
console.log("[ChiTiet] productId:", window.productId);
console.log("[ChiTiet] productVariants count:", window.productVariants.length);
console.log(
  "[ChiTiet] productVariants data:",
  JSON.stringify(window.productVariants, null, 2)
);

// ==================== CAROUSEL FOR SUGGESTED PRODUCTS ====================
window.moveSlide = function (direction) {
  const slides = document.querySelectorAll(".suggested-slide");
  if (slides.length === 0) return;
  slides[window.currentSlide].style.display = "none";
  window.currentSlide += direction;
  if (window.currentSlide < 0) window.currentSlide = slides.length - 1;
  if (window.currentSlide >= slides.length) window.currentSlide = 0;
  slides[window.currentSlide].style.display = "flex";
};

// ==================== TOAST NOTIFICATION SYSTEM ====================
function showNotification(message, type, title, duration) {
  type = type || "success";
  title = title || "";
  duration = duration || 4000;

  const container = document.getElementById("toastContainer");
  if (!container) return;

  const toast = document.createElement("div");
  toast.className = "toast " + type;

  const icons = {
    error: "fas fa-exclamation-triangle",
    warning: "fas fa-exclamation-circle",
    success: "fas fa-check-circle",
    info: "fas fa-info-circle",
  };

  const toastTitle =
    title ||
    (type === "success"
      ? "Thành công"
      : type === "error"
      ? "Lỗi"
      : type === "info"
      ? "Thông tin"
      : "Thông báo");

  toast.innerHTML =
    '<div class="toast-icon"><i class="' +
    icons[type] +
    '"></i></div>' +
    '<div class="toast-content">' +
    '<div class="toast-title">' +
    toastTitle +
    "</div>" +
    '<div class="toast-message">' +
    message +
    "</div>" +
    "</div>" +
    '<button class="toast-close"><i class="fas fa-times"></i></button>' +
    '<div class="toast-progress"></div>';

  container.appendChild(toast);

  setTimeout(function () {
    toast.classList.add("show");
  }, 100);

  toast.querySelector(".toast-close").addEventListener("click", function () {
    closeToast(toast);
  });

  setTimeout(function () {
    closeToast(toast);
  }, duration);
}

function closeToast(toast) {
  toast.classList.remove("show");
  setTimeout(function () {
    if (toast.parentNode) {
      toast.parentNode.removeChild(toast);
    }
  }, 300);
}

// ==================== UPDATE CART COUNT FROM API ====================
function updateCartCountFromAPI() {
  console.log("updateCartCountFromAPI called");

  // Gọi API để lấy số lượng giỏ hàng hiện tại
  fetch("/KhachHang/Cart/GetCartCount", {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success && typeof updateCartCount === "function") {
        updateCartCount(data.cartCount);
        console.log("Cart count updated to:", data.cartCount);
      }
    })
    .catch((error) => {
      console.error("Error updating cart count:", error);
    });
}

// ==================== PRODUCT VARIANT SELECTION ====================
window.selectOption = function (element, tenThuocTinh, idThuocTinh, idGiaTri) {
  element.classList.add("selecting");
  setTimeout(function () {
    element.classList.remove("selecting");
  }, 300);

  const group = element.parentElement;
  const isAlreadyActive = element.classList.contains("active");

  // Nếu item đã được chọn, bỏ chọn nó
  if (isAlreadyActive) {
    element.classList.remove("active");
    delete window.selectedAttributes[idThuocTinh];
    console.log("[SelectOption] Deselected:", idThuocTinh, idGiaTri);
  } else {
    // Bỏ chọn tất cả item khác trong group
    group.querySelectorAll(".option-item").forEach(function (item) {
      item.classList.remove("active");
    });

    // Chọn item hiện tại
    element.classList.add("active");
    window.selectedAttributes[idThuocTinh] = idGiaTri;
    console.log("[SelectOption] Selected:", idThuocTinh, idGiaTri);
  }

  findMatchingVariant();
  updateButtonStates();
};

function findMatchingVariant() {
  // Reset các biến về null khi bắt đầu tìm kiếm
  window.selectedVariantId = null;
  window.selectedVariantPrice = null;

  const totalAttributes = Object.keys(window.selectedAttributes).length;
  console.log(
    "[findMatchingVariant] Selected attributes count:",
    totalAttributes,
    "selectedAttributes:",
    window.selectedAttributes
  );
  console.log(
    "[findMatchingVariant] Total variants:",
    window.productVariants.length
  );

  for (var i = 0; i < window.productVariants.length; i++) {
    var variant = window.productVariants[i];
    console.log(
      "[findMatchingVariant] Checking variant:",
      variant.idBienThe,
      "Variant keys:",
      Object.keys(variant),
      "Full variant:",
      variant
    );

    // Kiểm tra cả PascalCase và camelCase
    var thuocTinhGiaTris = variant.thuocTinhGiaTris || variant.ThuocTinhGiaTris;
    console.log("[findMatchingVariant] thuocTinhGiaTris:", thuocTinhGiaTris);

    if (thuocTinhGiaTris && thuocTinhGiaTris.length === totalAttributes) {
      var match = true;

      for (var j = 0; j < thuocTinhGiaTris.length; j++) {
        var attr = thuocTinhGiaTris[j];
        // Kiểm tra cả camelCase và PascalCase cho property
        var idThuocTinh = attr.idThuocTinh || attr.IdThuocTinh;
        var idGiaTri = attr.idGiaTri || attr.IdGiaTri;

        console.log("[findMatchingVariant] Comparing attribute:", {
          idThuocTinh,
          idGiaTri,
          selected: window.selectedAttributes[idThuocTinh],
        });

        if (window.selectedAttributes[idThuocTinh] !== idGiaTri) {
          match = false;
          break;
        }
      }

      if (match) {
        window.selectedVariantId = variant.idBienThe || variant.IdBienThe;
        window.selectedVariantPrice = variant.giaBan || variant.GiaBan;
        console.log(
          "[findMatchingVariant] MATCHED! Variant ID:",
          window.selectedVariantId,
          "Price:",
          window.selectedVariantPrice
        );
        break;
      }
    }
  }

  if (!window.selectedVariantId) {
    console.log("[findMatchingVariant] No matching variant found");
  }

  updateSelectedVariantDisplay();
  updateAvailableOptions();
}

function updateSelectedVariantDisplay() {
  const infoDiv = document.getElementById("selectedVariantInfo");
  const textSpan = document.getElementById("selectedVariantText");
  const priceSpan = document.getElementById("selectedVariantPrice");
  const hiddenInput = document.getElementById("selectedVariantId");
  const displayPrice = document.getElementById("displayPrice");

  console.log(
    "[updateSelectedVariantDisplay] selectedVariantId:",
    window.selectedVariantId
  );
  console.log(
    "[updateSelectedVariantDisplay] selectedVariantPrice:",
    window.selectedVariantPrice
  );

  if (window.selectedVariantId) {
    var variantText = [];
    Object.keys(window.selectedAttributes).forEach(function (thuocTinhId) {
      const giaTriId = window.selectedAttributes[thuocTinhId];
      const optionElement = document.querySelector(
        '[data-thuoc-tinh-id="' +
          thuocTinhId +
          '"][data-gia-tri-id="' +
          giaTriId +
          '"]'
      );
      if (optionElement) {
        const thuocTinhName = optionElement
          .closest(".option-group")
          .querySelector(".option-label").textContent;
        variantText.push(thuocTinhName + ": " + optionElement.textContent);
      }
    });

    if (textSpan) textSpan.textContent = variantText.join(", ");

    // Tính giá sau khuyến mãi nếu có - RIÊNG cho biến thể này
    var finalPrice = window.selectedVariantPrice;
    var hasPromotion = false;

    // Kiểm tra xem có hàm calculatePromotionPrice không
    if (typeof window.calculatePromotionPrice === "function") {
      finalPrice = window.calculatePromotionPrice(window.selectedVariantPrice);
      hasPromotion = finalPrice < window.selectedVariantPrice;
      console.log(
        "[Variant Price] Original:",
        window.selectedVariantPrice,
        "Discounted:",
        finalPrice,
        "Has promotion:",
        hasPromotion
      );
    } else {
      console.log("[Variant Price] calculatePromotionPrice function not found");
    }

    if (priceSpan)
      priceSpan.textContent = finalPrice
        ? finalPrice.toLocaleString() + "₫"
        : "";
    if (hiddenInput) hiddenInput.value = window.selectedVariantId;
    if (infoDiv) infoDiv.style.display = "block";

    // Cập nhật giá chính khi chọn biến thể - hiển thị cả giá gốc và giá khuyến mãi
    if (displayPrice) {
      console.log(
        "[Display Update] Has promotion:",
        hasPromotion,
        "Final price:",
        finalPrice
      );
      if (hasPromotion) {
        // Có khuyến mãi - hiển thị giá giảm, giá gốc gạch ngang, và % giảm
        var discountPercent = Math.round(
          ((window.selectedVariantPrice - finalPrice) /
            window.selectedVariantPrice) *
            100
        );

        // Cập nhật giá hiển thị
        displayPrice.textContent = finalPrice.toLocaleString() + "₫";
        displayPrice.className = "promotion-price fs-3 fw-bold text-danger";

        // Hiển thị giá gốc
        var originalPriceSpan = document.getElementById("originalPrice");
        if (originalPriceSpan) {
          originalPriceSpan.textContent =
            window.selectedVariantPrice.toLocaleString() + "₫";
          originalPriceSpan.classList.remove("d-none");
          console.log(
            "[Display] Original price shown:",
            window.selectedVariantPrice.toLocaleString() + "₫"
          );
        }

        // Hiển thị % giảm
        var discountBadge = document.getElementById("discountBadge");
        if (discountBadge) {
          discountBadge.textContent = "-" + discountPercent + "%";
          discountBadge.classList.remove("d-none");
        }

        console.log(
          "[Display] Promotion price:",
          finalPrice.toLocaleString() + "₫",
          "Discount:",
          discountPercent + "%"
        );
      } else {
        // Không có khuyến mãi - chỉ hiển thị giá gốc
        displayPrice.textContent =
          window.selectedVariantPrice.toLocaleString() + "₫";
        displayPrice.className = "current-price fs-3 fw-bold";

        // Ẩn giá gốc và % giảm
        var originalPriceSpan = document.getElementById("originalPrice");
        if (originalPriceSpan) {
          originalPriceSpan.classList.add("d-none");
        }

        var discountBadge = document.getElementById("discountBadge");
        if (discountBadge) {
          discountBadge.classList.add("d-none");
        }

        console.log(
          "[Display] Regular price:",
          window.selectedVariantPrice.toLocaleString() + "₫"
        );
      }
    }

    var selectedVariant = window.productVariants.find(function (v) {
      return v.idBienThe === window.selectedVariantId;
    });
    if (selectedVariant) {
      const quantityInput = document.getElementById("productQuantity");
      if (quantityInput) {
        quantityInput.max = selectedVariant.soLuongTonKho;
      }

      const stockInfo = document.querySelector(".stock-info");
      if (stockInfo) {
        if (selectedVariant.soLuongTonKho > 0) {
          stockInfo.innerHTML =
            '<span class="stock-available">Còn ' +
            selectedVariant.soLuongTonKho +
            " sản phẩm</span>";
        } else {
          stockInfo.innerHTML = '<span class="stock-out">Hết hàng</span>';
        }
      }
    }
  } else {
    if (infoDiv) infoDiv.style.display = "none";
    if (hiddenInput) hiddenInput.value = "";
  }
}

function updateAvailableOptions() {
  // Logic disable option có thể được thêm sau
}

function updateButtonStates() {
  const addToCartBtn = document.querySelector(".btn-add-to-cart");
  const buyNowBtn = document.querySelector(".btn-buy-now");

  console.log(
    "[updateButtonStates] selectedVariantId:",
    window.selectedVariantId
  );

  if (!window.selectedVariantId) {
    if (addToCartBtn) {
      addToCartBtn.disabled = true;
      const hasIcon = addToCartBtn.querySelector("i");
      const iconHtml = hasIcon ? '<i class="fas fa-shopping-cart"></i> ' : "";
      addToCartBtn.innerHTML = iconHtml + "Vui lòng chọn loại sản phẩm";
    }
    if (buyNowBtn) {
      buyNowBtn.disabled = true;
      const hasIcon = buyNowBtn.querySelector("i");
      const iconHtml = hasIcon ? '<i class="fas fa-bolt"></i> ' : "";
      buyNowBtn.innerHTML = iconHtml + "Vui lòng chọn loại sản phẩm";
    }
  } else {
    var selectedVariant = window.productVariants.find(function (v) {
      return v.idBienThe === window.selectedVariantId;
    });
    if (selectedVariant && selectedVariant.soLuongTonKho > 0) {
      if (addToCartBtn) {
        addToCartBtn.disabled = false;
        addToCartBtn.innerHTML =
          '<i class="fas fa-shopping-cart"></i> Thêm giỏ hàng';
      }
      if (buyNowBtn) {
        buyNowBtn.disabled = false;
        buyNowBtn.innerHTML = '<i class="fas fa-bolt"></i> Mua Ngay';
      }
    } else {
      if (addToCartBtn) {
        addToCartBtn.disabled = true;
        addToCartBtn.innerHTML = "Mặt hàng này đã hết";
      }
      if (buyNowBtn) {
        buyNowBtn.disabled = true;
        buyNowBtn.innerHTML = "Mặt hàng này đã hết";
      }
    }
  }
}

// ==================== QUANTITY CONTROLS ====================
window.increaseQuantity = function () {
  var quantityInput = document.getElementById("productQuantity");
  if (!quantityInput) return;

  var currentValue = parseInt(quantityInput.value);

  if (window.maxQuantity <= 0) {
    showOutOfStockModal();
    return;
  }

  if (currentValue < window.maxQuantity) {
    quantityInput.value = currentValue + 1;
  } else {
    showNotification(
      "Không thể chọn quá " + window.maxQuantity + " sản phẩm!",
      "warning"
    );
  }
};

window.decreaseQuantity = function () {
  var quantityInput = document.getElementById("productQuantity");
  if (!quantityInput) return;

  var currentValue = parseInt(quantityInput.value);

  if (currentValue > 1) {
    quantityInput.value = currentValue - 1;
  }
};

window.validateQuantity = function () {
  var quantityInput = document.getElementById("productQuantity");
  if (!quantityInput) return;

  var value = parseInt(quantityInput.value);

  if (isNaN(value) || value < 1) {
    quantityInput.value = 1;
  } else if (value > window.maxQuantity) {
    quantityInput.value = window.maxQuantity;
    showNotification(
      "Không thể chọn quá " + window.maxQuantity + " sản phẩm!",
      "warning"
    );
  }
};

// ==================== ADD TO CART FUNCTIONS ====================
window.addToCart = function (productId) {
  console.log(
    "[addToCart] Called with productId:",
    productId,
    "selectedVariantId:",
    window.selectedVariantId
  );

  if (!window.selectedVariantId) {
    showNotification(
      "Vui lòng chọn loại sản phẩm trước khi thêm vào giỏ hàng!",
      "error"
    );
    return;
  }

  var quantity = parseInt(document.getElementById("productQuantity").value);
  var formData = new FormData();
  formData.append("productId", productId);
  formData.append("variantId", window.selectedVariantId);
  formData.append("quantity", quantity);

  var token = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  );
  if (token) {
    formData.append("__RequestVerificationToken", token.value);
  }

  fetch("/KhachHang/ChiTiet/ThemVaoGioHang", {
    method: "POST",
    body: formData,
  })
    .then(function (response) {
      return response.json();
    })
    .then(function (data) {
      if (data.success) {
        // Cập nhật số lượng giỏ hàng ngay từ response
        if (
          data.cartCount !== undefined &&
          typeof updateCartCount === "function"
        ) {
          updateCartCount(data.cartCount);
          console.log("Cart count updated to:", data.cartCount);
        } else if (typeof updateCartCountFromAPI === "function") {
          // Fallback nếu không có cartCount trong response
          updateCartCountFromAPI();
        }
      } else {
        showNotification(
          data.message || "Có lỗi xảy ra khi thêm vào giỏ hàng",
          "error"
        );
      }
    })
    .catch(function (error) {
      console.error("Error:", error);
      showNotification("Có lỗi xảy ra khi thêm vào giỏ hàng", "error");
    });
};

window.addToCartWithoutVariant = function (productId) {
  var quantity = parseInt(document.getElementById("productQuantity").value);
  var formData = new FormData();
  formData.append("productId", productId);
  formData.append("variantId", 0);
  formData.append("quantity", quantity);

  var token = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  );
  if (token) {
    formData.append("__RequestVerificationToken", token.value);
  }

  fetch("/KhachHang/ChiTiet/ThemVaoGioHang", {
    method: "POST",
    body: formData,
  })
    .then(function (response) {
      return response.json();
    })
    .then(function (data) {
      if (data.success) {
        // Cập nhật số lượng giỏ hàng ngay từ response
        if (
          data.cartCount !== undefined &&
          typeof updateCartCount === "function"
        ) {
          updateCartCount(data.cartCount);
          console.log("Cart count updated to:", data.cartCount);
        } else if (typeof updateCartCountFromAPI === "function") {
          // Fallback nếu không có cartCount trong response
          updateCartCountFromAPI();
        }
      } else {
        showNotification(
          data.message || "Có lỗi xảy ra khi thêm vào giỏ hàng",
          "error"
        );
      }
    })
    .catch(function (error) {
      console.error("Error:", error);
      showNotification("Có lỗi xảy ra khi thêm vào giỏ hàng", "error");
    });
};

window.addToCartWithAutoVariant = function (productId, variantId) {
  var quantity = parseInt(document.getElementById("productQuantity").value);
  var formData = new FormData();
  formData.append("productId", productId);
  formData.append("variantId", variantId);
  formData.append("quantity", quantity);

  var token = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  );
  if (token) {
    formData.append("__RequestVerificationToken", token.value);
  }

  fetch("/KhachHang/ChiTiet/ThemVaoGioHang", {
    method: "POST",
    body: formData,
  })
    .then(function (response) {
      return response.json();
    })
    .then(function (data) {
      if (data.success) {
        // Cập nhật số lượng giỏ hàng ngay từ response
        if (
          data.cartCount !== undefined &&
          typeof updateCartCount === "function"
        ) {
          updateCartCount(data.cartCount);
          console.log("Cart count updated to:", data.cartCount);
        } else if (typeof updateCartCountFromAPI === "function") {
          // Fallback nếu không có cartCount trong response
          updateCartCountFromAPI();
        }
      } else {
        showNotification(
          data.message || "Có lỗi xảy ra khi thêm vào giỏ hàng",
          "error"
        );
      }
    })
    .catch(function (error) {
      console.error("Error:", error);
      showNotification("Có lỗi xảy ra khi thêm vào giỏ hàng", "error");
    });
};

// ==================== BUY NOW FUNCTIONS ====================
window.buyNow = function (productId) {
  console.log(
    "[buyNow] Called with productId:",
    productId,
    "selectedVariantId:",
    window.selectedVariantId
  );

  if (!window.selectedVariantId) {
    showNotification("Vui lòng chọn loại sản phẩm trước khi mua!", "error");
    return;
  }

  var quantity = parseInt(document.getElementById("productQuantity").value);
  var url =
    "/KhachHang/ChiTiet/MuaNgay?productId=" +
    productId +
    "&variantId=" +
    window.selectedVariantId +
    "&quantity=" +
    quantity;
  console.log("[buyNow] Redirecting to:", url);
  window.location.href = url;
};

window.buyNowWithVariant = function () {
  console.log(
    "[buyNowWithVariant] selectedVariantId:",
    window.selectedVariantId
  );

  if (!window.selectedVariantId) {
    showNotification("Vui lòng chọn loại sản phẩm trước khi mua!", "error");
    return;
  }

  var quantity = parseInt(document.getElementById("productQuantity").value);
  var url =
    "/KhachHang/ChiTiet/MuaNgay?productId=" +
    window.productId +
    "&variantId=" +
    window.selectedVariantId +
    "&quantity=" +
    quantity;
  console.log("[buyNowWithVariant] Redirecting to:", url);
  window.location.href = url;
};

window.buyNowWithoutVariant = function (productId) {
  var quantity = parseInt(document.getElementById("productQuantity").value);
  var url =
    "/KhachHang/ChiTiet/MuaNgay?productId=" +
    productId +
    "&variantId=0&quantity=" +
    quantity;
  window.location.href = url;
};

window.buyNowWithAutoVariant = function (productId, variantId) {
  var quantity = parseInt(document.getElementById("productQuantity").value);
  var url =
    "/KhachHang/ChiTiet/MuaNgay?productId=" +
    productId +
    "&variantId=" +
    variantId +
    "&quantity=" +
    quantity;
  window.location.href = url;
};

// ==================== IMAGE MODAL FUNCTIONS ====================
window.changeMainImage = function (src) {
  var mainImage = document.getElementById("mainImage");
  if (mainImage) {
    mainImage.src = src;
  }

  var thumbnails = document.querySelectorAll(".thumbnail");
  thumbnails.forEach(function (thumb) {
    thumb.classList.remove("active");
    if (thumb.src === src) {
      thumb.classList.add("active");
    }
  });
};

window.openImageModal = function (src) {
  var modal = document.getElementById("imageModal");
  var modalImg = document.getElementById("modalImage");
  if (modal && modalImg) {
    modal.style.display = "flex";
    modalImg.src = src;
  }
};

window.closeImageModal = function () {
  var modal = document.getElementById("imageModal");
  if (modal) {
    modal.style.display = "none";
  }
};

// ==================== OUT OF STOCK MODAL ====================
window.showOutOfStockModal = function () {
  var modal = document.getElementById("outOfStockModal");
  if (modal) {
    modal.style.display = "flex";
    document.body.style.overflow = "hidden";
  }
};

window.closeOutOfStockModal = function () {
  var modal = document.getElementById("outOfStockModal");
  if (modal) {
    modal.style.display = "none";
    document.body.style.overflow = "auto";
  }
};

// ==================== INITIALIZATION ====================
document.addEventListener("DOMContentLoaded", function () {
  var thumbnails = document.querySelectorAll(".thumbnail");
  thumbnails.forEach(function (thumb) {
    thumb.addEventListener("click", function () {
      changeMainImage(thumb.src);
    });
  });

  var mainImage = document.getElementById("mainImage");
  if (mainImage) {
    mainImage.addEventListener("click", function () {
      openImageModal(mainImage.src);
    });
  }

  var modal = document.getElementById("imageModal");
  if (modal) {
    modal.addEventListener("click", function (e) {
      if (e.target === modal || e.target.className === "close-modal") {
        closeImageModal();
      }
    });
  }

  var quantityInput = document.getElementById("productQuantity");
  if (quantityInput) {
    quantityInput.addEventListener("blur", validateQuantity);
    quantityInput.addEventListener("input", validateQuantity);
  }

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      closeImageModal();
      closeOutOfStockModal();
    }
  });

  updateButtonStates();

  // Initialize review features
  initializeReviewFeatures();

  // Handle review form submit
  var reviewForm = document.getElementById("reviewForm");
  if (reviewForm) {
    reviewForm.addEventListener("submit", function (e) {
      console.log("Form submitting...");

      // Kiểm tra validation
      var rating = document.querySelector('input[name="rating"]:checked');
      var comment = document.getElementById("reviewComment");

      if (!rating) {
        e.preventDefault();
        showNotification("Vui lòng chọn số sao đánh giá!", "error");
        return false;
      }

      if (!comment || !comment.value.trim()) {
        e.preventDefault();
        showNotification("Vui lòng nhập mô tả nhận xét!", "error");
        return false;
      }

      // Kiểm tra file input
      var fileInput = document.getElementById("reviewImages");
      if (fileInput && fileInput.files) {
        console.log("Submitting with", fileInput.files.length, "images");
      }

      console.log("Form validation passed");
      return true;
    });
  }
});

// ==================== REVIEW FEATURES ====================
function initializeReviewFeatures() {
  // Character counter for review comment
  var reviewComment = document.getElementById("reviewComment");
  if (reviewComment) {
    reviewComment.addEventListener("input", function () {
      var charCount = document.getElementById("charCount");
      if (charCount) {
        charCount.textContent = this.value.length;
      }
    });
  }

  // Star rating hover và click effect
  var starInputs = document.querySelectorAll(".star-rating-input label");
  var ratingText = document.querySelector(".rating-text");

  var ratingTexts = {
    5: "Rất hài lòng",
    4: "Hài lòng",
    3: "Bình thường",
    2: "Không hài lòng",
    1: "Rất tệ",
  };

  starInputs.forEach(function (label) {
    // Hover effect
    label.addEventListener("mouseenter", function () {
      var rating = this.getAttribute("for").replace("star", "");
      if (ratingText) {
        ratingText.textContent = ratingTexts[rating] || "Chưa chọn";
      }
    });

    // Click effect
    label.addEventListener("click", function () {
      var rating = this.getAttribute("for").replace("star", "");
      if (ratingText) {
        ratingText.textContent = ratingTexts[rating] || "Chưa chọn";
      }
    });
  });

  // Reset text when mouse leaves
  var starRatingDiv = document.querySelector(".star-rating-input");
  if (starRatingDiv) {
    starRatingDiv.addEventListener("mouseleave", function () {
      var checkedInput = document.querySelector(
        ".star-rating-input input:checked"
      );
      if (checkedInput && ratingText) {
        var rating = checkedInput.value;
        ratingText.textContent = ratingTexts[rating] || "Chưa chọn";
      } else if (ratingText) {
        ratingText.textContent = "Chưa chọn";
      }
    });
  }
}

// Toggle Review Form (mở/đóng form đánh giá)
window.toggleReviewForm = function (show) {
  var trigger = document.getElementById("reviewTrigger");
  var container = document.getElementById("reviewFormContainer");
  var alreadyReviewedMsg = document.querySelector(".already-reviewed");

  if (show) {
    // Mở form
    if (trigger) trigger.style.display = "none";
    if (alreadyReviewedMsg) alreadyReviewedMsg.style.display = "none";
    if (container) container.style.display = "block";

    // Scroll to form
    setTimeout(function () {
      if (container) {
        container.scrollIntoView({ behavior: "smooth", block: "center" });
      }
    }, 100);
  } else {
    // Đóng form (nút Bỏ qua)
    if (trigger) trigger.style.display = "block";
    if (alreadyReviewedMsg) alreadyReviewedMsg.style.display = "block";
    if (container) container.style.display = "none";

    // Reset form
    var form = document.getElementById("reviewForm");
    if (form) {
      form.reset();

      // Xóa editReviewId nếu có (quan trọng cho chức năng edit)
      var editIdInput = form.querySelector('input[name="editReviewId"]');
      if (editIdInput) {
        editIdInput.remove();
      }

      // Reset text nút submit về mặc định
      var submitBtn = form.querySelector('button[type="submit"]');
      if (submitBtn) {
        submitBtn.innerHTML = '<i class="fas fa-paper-plane"></i> Gửi';
      }

      // Reset rating text
      var ratingText = document.querySelector(".rating-text");
      if (ratingText) {
        ratingText.textContent = "Chưa chọn";
      }

      // Clear image preview
      var imagePreview = document.getElementById("imagePreview");
      if (imagePreview) {
        imagePreview.innerHTML = "";
      }

      // Reset selectedFiles array
      selectedFiles = [];

      // Reset char count
      var charCount = document.getElementById("charCount");
      if (charCount) {
        charCount.textContent = "0";
      }
    }
  }
};

// ==================== IMAGE UPLOAD MANAGEMENT ====================
// Lưu trữ danh sách file đã chọn
var selectedFiles = [];

// Preview images before upload
window.previewImages = function (event) {
  var fileInput = document.getElementById("reviewImages");
  var newFiles = Array.from(event.target.files);

  if (!fileInput || newFiles.length === 0) return;

  // Kiểm tra nếu đã đủ 5 ảnh
  if (selectedFiles.length >= 5) {
    showNotification(
      "Đã đủ 5 ảnh. Vui lòng xóa bớt nếu muốn thay đổi.",
      "warning"
    );
    // Reset file input về rỗng
    fileInput.value = "";
    return;
  }

  var addedCount = 0;
  var duplicateCount = 0;
  var exceededCount = 0;

  // Thêm file mới vào danh sách (kiểm tra trùng lặp)
  newFiles.forEach(function (file) {
    // Kiểm tra đã đủ 5 ảnh chưa
    if (selectedFiles.length >= 5) {
      exceededCount++;
      return;
    }

    // Kiểm tra file có phải ảnh không
    if (!file.type.match("image.*")) {
      return;
    }

    // Kiểm tra trùng lặp (theo tên file và kích thước)
    var isDuplicate = selectedFiles.some(function (existingFile) {
      return existingFile.name === file.name && existingFile.size === file.size;
    });

    if (isDuplicate) {
      duplicateCount++;
      return;
    }

    // Thêm file mới
    selectedFiles.push(file);
    addedCount++;
  });

  // Cập nhật file input với danh sách đã lọc
  updateFileInput();

  // Render preview
  renderImagePreviews();

  // Hiển thị thông báo
  if (exceededCount > 0) {
    showNotification(
      "Chỉ được chọn tối đa 5 ảnh. Đã bỏ qua " + exceededCount + " ảnh.",
      "warning"
    );
  } else if (duplicateCount > 0) {
    showNotification("Đã bỏ qua " + duplicateCount + " ảnh trùng lặp.", "info");
  } else if (addedCount > 0) {
    showNotification("Đã thêm " + addedCount + " ảnh.", "success");
  }
};

// Cập nhật file input từ selectedFiles array
function updateFileInput() {
  var fileInput = document.getElementById("reviewImages");
  if (!fileInput) return;

  try {
    var dt = new DataTransfer();
    selectedFiles.forEach(function (file) {
      dt.items.add(file);
    });
    fileInput.files = dt.files;
  } catch (error) {
    console.error("Error updating file input:", error);
  }
}

// Render image previews từ selectedFiles
function renderImagePreviews() {
  var previewContainer = document.getElementById("imagePreview");
  if (!previewContainer) return;

  previewContainer.innerHTML = "";

  // Thêm header hiển thị số ảnh
  if (selectedFiles.length > 0) {
    var header = document.createElement("div");
    header.className = "preview-header";
    header.style.cssText =
      "margin-bottom: 10px; font-weight: 500; color: #666;";
    header.innerHTML = "Đã chọn " + selectedFiles.length + "/5 ảnh";
    previewContainer.appendChild(header);
  }

  selectedFiles.forEach(function (file, index) {
    var reader = new FileReader();
    reader.onload = function (e) {
      var div = document.createElement("div");
      div.className = "preview-item";
      div.innerHTML =
        '<img src="' +
        e.target.result +
        '" alt="Preview">' +
        '<button type="button" class="preview-remove" onclick="removePreviewImage(' +
        index +
        ')">' +
        '<i class="fas fa-times"></i>' +
        "</button>";
      previewContainer.appendChild(div);
    };
    reader.readAsDataURL(file);
  });
}

// Remove preview image
window.removePreviewImage = function (index) {
  // Xóa file khỏi array
  selectedFiles.splice(index, 1);

  // Cập nhật file input
  updateFileInput();

  // Re-render previews
  renderImagePreviews();

  // Thông báo
  if (selectedFiles.length === 0) {
    showNotification("Đã xóa tất cả ảnh.", "info");
  } else {
    showNotification(
      "Đã xóa ảnh. Còn lại " + selectedFiles.length + "/5 ảnh.",
      "info"
    );
  }
};

// Reset review form
window.resetReviewForm = function () {
  // Sử dụng toggleReviewForm để đóng form
  toggleReviewForm(false);
};

// Filter reviews by rating
window.filterReviews = function (filter) {
  console.log("[FilterReviews] Filter:", filter);

  var reviewItems = document.querySelectorAll(".review-item");
  var filterBtns = document.querySelectorAll(".filter-btn");

  console.log("[FilterReviews] Total reviews:", reviewItems.length);

  // Update active button
  filterBtns.forEach(function (btn) {
    btn.classList.remove("active");
  });
  event.target.closest(".filter-btn").classList.add("active");

  var visibleCount = 0;

  reviewItems.forEach(function (item) {
    var rating = item.getAttribute("data-rating");
    var hasImageAttr = item.getAttribute("data-has-image");
    var hasImage = hasImageAttr === "true"; // So sánh với string 'true'

    console.log(
      "[FilterReviews] Review - Rating:",
      rating,
      "HasImage:",
      hasImageAttr,
      "Parsed:",
      hasImage
    );

    var shouldShow = false;

    if (filter === "all") {
      shouldShow = true;
    } else if (filter === "images") {
      shouldShow = hasImage;
    } else {
      shouldShow = rating == filter;
    }

    if (shouldShow) {
      item.style.display = "block";
      visibleCount++;
    } else {
      item.style.display = "none";
    }
  });

  console.log("[FilterReviews] Visible reviews:", visibleCount);

  // Check if no reviews match filter
  var reviewsList = document.getElementById("reviewsList");
  if (reviewsList) {
    // Tìm thông báo ban đầu (không phải filter)
    var originalNoReviews = reviewsList.querySelector(
      ".no-reviews:not(.no-filter-results)"
    );
    // Tìm thông báo do filter tạo ra
    var filterNoReviews = reviewsList.querySelector(".no-filter-results");

    if (visibleCount === 0) {
      // Nếu không có review nào hiển thị
      if (originalNoReviews) {
        // Nếu có thông báo ban đầu, giữ nguyên và ẩn nó
        originalNoReviews.style.display = "none";
      }

      if (!filterNoReviews) {
        // Tạo thông báo filter mới
        var div = document.createElement("div");
        div.className = "no-reviews no-filter-results";

        var filterText =
          filter === "images"
            ? "có hình ảnh"
            : filter === "all"
            ? ""
            : filter + " sao";

        div.innerHTML =
          '<i class="fas fa-comment-slash"></i>' +
          "<p>Chưa có đánh giá nào " +
          filterText +
          " cho sản phẩm này</p>";
        reviewsList.appendChild(div);
      } else {
        // Cập nhật text của thông báo filter
        var filterText =
          filter === "images"
            ? "có hình ảnh"
            : filter === "all"
            ? ""
            : filter + " sao";

        filterNoReviews.querySelector("p").textContent =
          "Chưa có đánh giá nào " + filterText + " cho sản phẩm này";
        filterNoReviews.style.display = "block";
      }
    } else {
      // Có review hiển thị
      if (filterNoReviews) {
        // Xóa thông báo filter
        filterNoReviews.remove();
      }

      if (originalNoReviews) {
        // Ẩn thông báo ban đầu vì đã có review
        originalNoReviews.style.display = "none";
      }
    }

    // Nếu filter = 'all' và có review, hiện lại thông báo ban đầu nếu không có review nào
    if (filter === "all" && originalNoReviews && reviewItems.length === 0) {
      originalNoReviews.style.display = "block";
      if (filterNoReviews) {
        filterNoReviews.remove();
      }
    }
  }
};

// ==================== EDIT REVIEW FUNCTION ====================
window.editReview = function (buttonElement) {
  console.log("[EditReview] Function called");
  console.log("[EditReview] Button element:", buttonElement);

  // Lấy dữ liệu từ data attributes
  const reviewId = buttonElement.getAttribute("data-review-id");
  const currentRating = parseInt(buttonElement.getAttribute("data-rating"));
  const currentComment = buttonElement.getAttribute("data-comment");
  const currentImages = buttonElement.getAttribute("data-images");

  console.log("[EditReview] Review data:", {
    reviewId,
    currentRating,
    currentComment,
    currentImages,
  });

  // Hiển thị form đánh giá
  console.log("[EditReview] Calling toggleReviewForm(true)");
  toggleReviewForm(true);

  // Điền thông tin cũ vào form
  // Set rating
  const ratingInput = document.querySelector(
    `input[name="rating"][value="${currentRating}"]`
  );
  console.log("[EditReview] Rating input found:", ratingInput);

  if (ratingInput) {
    ratingInput.checked = true;
    // Update rating text
    const ratingText = document.querySelector(".rating-text");
    if (ratingText) {
      const ratingTexts = {
        5: "Tuyệt vời",
        4: "Hài lòng",
        3: "Bình thường",
        2: "Không hài lòng",
        1: "Rất tệ",
      };
      ratingText.textContent = ratingTexts[currentRating] || "Chưa chọn";
    }
  }

  // Set comment
  const commentTextarea = document.querySelector('textarea[name="comment"]');
  console.log("[EditReview] Comment textarea found:", commentTextarea);

  if (commentTextarea) {
    commentTextarea.value = currentComment || "";
    // Update character count
    const charCount = document.getElementById("charCount");
    if (charCount) {
      charCount.textContent = currentComment ? currentComment.length : 0;
    }
  }

  // Thêm hidden field để backend biết là edit
  const form = document.getElementById("reviewForm");
  console.log("[EditReview] Form found:", form);

  if (form) {
    // Xóa hidden field cũ nếu có
    const oldEditId = form.querySelector('input[name="editReviewId"]');
    if (oldEditId) {
      oldEditId.remove();
      console.log("[EditReview] Removed old editReviewId input");
    }

    // Thêm hidden field mới
    const editIdInput = document.createElement("input");
    editIdInput.type = "hidden";
    editIdInput.name = "editReviewId";
    editIdInput.value = reviewId;
    form.appendChild(editIdInput);
    console.log("[EditReview] Added new editReviewId input:", reviewId);

    // Thay đổi text nút submit
    const submitBtn = form.querySelector('button[type="submit"]');
    if (submitBtn) {
      submitBtn.innerHTML =
        '<i class="fas fa-paper-plane"></i> Cập nhật đánh giá';
      console.log("[EditReview] Updated submit button text");
    }
  }

  // Scroll to form
  const reviewFormContainer = document.getElementById("reviewFormContainer");
  if (reviewFormContainer) {
    setTimeout(() => {
      reviewFormContainer.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
      console.log("[EditReview] Scrolled to form");
    }, 200);
  }

  console.log("[EditReview] Function completed");
};

// ==================== SETUP EDIT REVIEW BUTTONS ====================
document.addEventListener("DOMContentLoaded", function () {
  console.log("[ChiTiet] DOM loaded, setting up edit buttons");

  // Attach event listeners to all edit buttons
  const editButtons = document.querySelectorAll(".btn-edit-review");
  console.log("[ChiTiet] Found", editButtons.length, "edit buttons");

  editButtons.forEach(function (button) {
    button.addEventListener("click", function (e) {
      e.preventDefault();
      console.log("[ChiTiet] Edit button clicked");
      editReview(this);
    });
  });
});

// ==================== COPY PRODUCT INFO AND OPEN TAWK.TO CHAT ====================
// Hàm sao chép thông tin sản phẩm (KHÔNG mở chat)
window.copyProductInfoAndOpenChat = function () {
  console.log("[CopyProductInfo] Function called");

  // Lấy thông tin sản phẩm
  if (typeof productInfo === "undefined") {
    console.error("[CopyProductInfo] Product info not found");
    showNotification("Không thể lấy thông tin sản phẩm", "error");
    return;
  }

  // Tạo text thông tin sản phẩm
  var productText = "🛍️ THÔNG TIN SẢN PHẨM\n";
  productText += "━━━━━━━━━━━━━━\n";
  productText += "📦 Tên: " + productInfo.name + "\n";
  productText += "💰 Giá: " + productInfo.price + "\n";
  productText += "📂 Danh mục: " + productInfo.category + "\n";

  // Thêm thông tin biến thể nếu có
  if (selectedVariantId) {
    var variant = productVariants.find(function (v) {
      return v.idBienThe === selectedVariantId;
    });
    if (
      variant &&
      variant.thuocTinhGiaTris &&
      variant.thuocTinhGiaTris.length > 0
    ) {
      productText += "━━━━━━━━━━━━━━\n";
      productText += "🎨 ";
      var attrs = [];
      variant.thuocTinhGiaTris.forEach(function (attr) {
        attrs.push(attr.tenThuocTinh + " - " + attr.giaTri);
      });
      productText += attrs.join(", ") + "\n";
    }
  }

  // Copy vào clipboard (KHÔNG mở chat)
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard
      .writeText(productText)
      .then(function () {
        console.log("[CopyProductInfo] Copied successfully");
        showNotification("Đã sao chép thông tin sản phẩm!", "success");
      })
      .catch(function (err) {
        console.error("[CopyProductInfo] Copy failed:", err);
        // Fallback method
        fallbackCopyTextToClipboard(productText);
      });
  } else {
    // Fallback for older browsers
    fallbackCopyTextToClipboard(productText);
  }
};

// Fallback copy method for older browsers
function fallbackCopyTextToClipboard(text) {
  var textArea = document.createElement("textarea");
  textArea.value = text;
  textArea.style.position = "fixed";
  textArea.style.top = "0";
  textArea.style.left = "0";
  textArea.style.width = "2em";
  textArea.style.height = "2em";
  textArea.style.padding = "0";
  textArea.style.border = "none";
  textArea.style.outline = "none";
  textArea.style.boxShadow = "none";
  textArea.style.background = "transparent";

  document.body.appendChild(textArea);
  textArea.focus();
  textArea.select();

  try {
    var successful = document.execCommand("copy");
    if (successful) {
      console.log("[CopyProductInfo] Fallback copy successful");
      showNotification("Đã sao chép thông tin sản phẩm!", "success");
    } else {
      console.error("[CopyProductInfo] Fallback copy failed");
      showNotification("Không thể sao chép. Vui lòng thử lại!", "error");
    }
  } catch (err) {
    console.error("[CopyProductInfo] Fallback copy error:", err);
    showNotification("Không thể sao chép. Vui lòng thử lại!", "error");
  }

  document.body.removeChild(textArea);
}

// Function to open Tawk.to chat widget
function openTawkToChat() {
  console.log("[OpenTawkToChat] Attempting to open chat");

  // Kiểm tra xem Tawk.to có được load không
  if (typeof Tawk_API !== "undefined" && Tawk_API.maximize) {
    try {
      Tawk_API.maximize();
      console.log("[OpenTawkToChat] Chat maximized successfully");
    } catch (err) {
      console.error("[OpenTawkToChat] Error maximizing chat:", err);
      showNotification("Vui lòng cuộn xuống để mở chat", "info");
    }
  } else {
    console.warn("[OpenTawkToChat] Tawk.to not loaded yet");
    showNotification("Đang tải chat... Vui lòng đợi một chút", "info");

    // Thử lại sau 2 giây
    setTimeout(function () {
      if (typeof Tawk_API !== "undefined" && Tawk_API.maximize) {
        try {
          Tawk_API.maximize();
          console.log("[OpenTawkToChat] Chat maximized on retry");
        } catch (err) {
          console.error("[OpenTawkToChat] Error on retry:", err);
        }
      } else {
        console.warn(
          "[OpenTawkToChat] Tawk.to still not available after retry"
        );
      }
    }, 2000);
  }
}
