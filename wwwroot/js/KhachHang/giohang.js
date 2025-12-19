// Utility functions
function formatVND(n) {
  return n.toLocaleString("vi-VN") + " ₫";
}

// LocalStorage functions for cart state
function saveCartState() {
  const cartRows = document.querySelectorAll("#cartBodyRazor tr[data-row-index]");
  const selectedCheckboxes = document.querySelectorAll(".cart-select-item:checked");
  
  // Save selected item IDs
  const selectedIds = Array.from(selectedCheckboxes).map(checkbox => 
    checkbox.getAttribute("data-row-index")
  );
  
  // Save cart totals
  const subtotalElement = document.getElementById("subtotal");
  const totalElement = document.getElementById("total");
  
  const cartState = {
    selectedIds: selectedIds,
    subtotal: subtotalElement ? subtotalElement.textContent : "0 ₫",
    total: totalElement ? totalElement.textContent : "0 ₫",
    totalItems: cartRows.length,
    selectedCount: selectedCheckboxes.length,
    timestamp: Date.now()
  };
  
  localStorage.setItem("cartState", JSON.stringify(cartState));
  console.log("[CartState] Saved:", cartState);
}

function loadCartState() {
  const savedState = localStorage.getItem("cartState");
  if (!savedState) return;
  
  try {
    const cartState = JSON.parse(savedState);
    console.log("[CartState] Loading:", cartState);
    
    // Restore selected checkboxes only
    if (cartState.selectedIds && cartState.selectedIds.length > 0) {
      cartState.selectedIds.forEach(rowIndex => {
        const checkbox = document.querySelector(`.cart-select-item[data-row-index="${rowIndex}"]`);
        if (checkbox) {
          checkbox.checked = true;
        }
      });
    }
    
    // Totals will be recalculated from actual cart data in DOM (from database)
    console.log("[CartState] Restored selections, totals will be recalculated from database");
    
  } catch (error) {
    console.error("[CartState] Error loading state:", error);
    localStorage.removeItem("cartState");
  }
}

function clearCartState() {
  localStorage.removeItem("cartState");
  console.log("[CartState] Cleared");
}

// DOM-based cart functions
function updateCartTotals() {
  const cartRows = document.querySelectorAll(
    "#cartBodyRazor tr[data-row-index]"
  );
  const selectedCheckboxes = document.querySelectorAll(
    ".cart-select-item:checked"
  );
  let subtotal = 0;
  let totalItems = cartRows.length;
  let selectedCount = selectedCheckboxes.length;

  // Calculate subtotal for selected items
  selectedCheckboxes.forEach((checkbox) => {
    const rowIndex = checkbox.getAttribute("data-row-index");
    const row = document.querySelector(`tr[data-row-index="${rowIndex}"]`);
    if (row) {
      const totalCell = row.querySelector(".cart-product-total");
      const totalText = totalCell.textContent.replace(/[^\d]/g, "");
      const total = parseInt(totalText, 10) || 0;
      subtotal += total;
    }
  });

  // Update cart title
  const cartTitle = document.getElementById("cartTitle");
  if (totalItems === 0) {
    cartTitle.textContent = "Giỏ hàng trống";
  } else {
    cartTitle.textContent = `Giỏ hàng (${totalItems} sản phẩm) - Đã chọn (${selectedCount} sản phẩm)`;
  }

  // Update totals
  document.getElementById("subtotal").textContent = formatVND(subtotal);
  document.getElementById("total").textContent = formatVND(subtotal);

  // Update select all button
  updateSelectAllButton();

  // Update delete all button visibility
  updateDeleteAllButton();
  
  // Save cart state to localStorage
  saveCartState();
}

function updateSelectAllButton() {
  const cartRows = document.querySelectorAll(
    "#cartBodyRazor tr[data-row-index]"
  );
  const selectedCheckboxes = document.querySelectorAll(
    ".cart-select-item:checked"
  );
  const btnSelectAll = document.getElementById("btnSelectAll");

  if (cartRows.length === 0) {
    btnSelectAll.style.display = "none";
    return;
  }

  btnSelectAll.style.display = "";

  if (selectedCheckboxes.length === cartRows.length) {
    btnSelectAll.textContent = "Bỏ chọn tất cả";
    btnSelectAll.className = "btn btn-sm btn-outline-secondary";
  } else {
    btnSelectAll.textContent = "Chọn tất cả";
    btnSelectAll.className = "btn btn-sm btn-outline-primary";
  }
}

function updateDeleteAllButton() {
  const cartRows = document.querySelectorAll(
    "#cartBodyRazor tr[data-row-index]"
  );
  const selectedCheckboxes = document.querySelectorAll(
    ".cart-select-item:checked"
  );
  const btnDeleteAll = document.getElementById("btnDeleteAll");

  if (selectedCheckboxes.length === cartRows.length && cartRows.length > 0) {
    btnDeleteAll.style.display = "";
  } else {
    btnDeleteAll.style.display = "none";
  }
}

async function updateRowTotal(rowIndex) {
  const row = document.querySelector(`tr[data-row-index="${rowIndex}"]`);
  if (!row) return;

  const priceElement = row.querySelector(".cart-product-price");
  const quantityInput = row.querySelector(".quantity-input");
  const totalElement = row.querySelector(".cart-product-total");
  const idBienThe = row.getAttribute("data-id-bien-the");

  const price = parseInt(priceElement.getAttribute("data-price"), 10) || 0;
  const quantity = parseInt(quantityInput.value, 10) || 1;
  const total = price * quantity;

  totalElement.textContent = formatVND(total);
  totalElement.setAttribute("data-total", total);

  // Gửi request lên server để lưu số lượng mới
  if (idBienThe) {
    try {
      const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
      const response = await fetch("/KhachHang/Cart/UpdateQuantity", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          "RequestVerificationToken": token
        },
        body: new URLSearchParams({
          id: idBienThe,
          quantity: quantity,
          __RequestVerificationToken: token
        })
      });

      if (!response.ok) {
        console.error("[UpdateQuantity] Failed to update quantity on server");
      }
    } catch (error) {
      console.error("[UpdateQuantity] Error:", error);
    }
  }

  updateCartTotals();
  updateCartCount();
}


async function deleteCartRow(rowIndex) {
  const row = document.querySelector(`tr[data-row-index="${rowIndex}"]`);
  if (row) {
    // Lấy ID biến thể để gọi API xóa
    const idBienThe = row.getAttribute("data-id-bien-the");

    try {
      // Lấy CSRF token
      const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
      )?.value;

      // Gọi API để xóa sản phẩm khỏi database
      const response = await fetch("/KhachHang/Cart/RemoveFromCart", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: `id=${
          idBienThe || 0
        }&__RequestVerificationToken=${encodeURIComponent(token || "")}`,
      });

      if (response.ok) {
        // Nếu API thành công, mới xóa khỏi DOM
        row.remove();

        // Re-index remaining rows
        const remainingRows = document.querySelectorAll(
          "#cartBodyRazor tr[data-row-index]"
        );
        remainingRows.forEach((row, newIndex) => {
          row.setAttribute("data-row-index", newIndex);

          // Update all data-row-index attributes in the row
          const elementsWithIndex = row.querySelectorAll("[data-row-index]");
          elementsWithIndex.forEach((element) => {
            element.setAttribute("data-row-index", newIndex);
          });
        });

        // Check if cart is empty
        if (remainingRows.length === 0) {
          const cartBody = document.getElementById("cartBodyRazor");
          cartBody.innerHTML =
            '<tr><td colspan="6" style="text-align:center; color:#888; font-size:16px;">Không có sản phẩm nào trong giỏ hàng.</td></tr>';
          document.getElementById("btnSelectAll").style.display = "none";
          document.getElementById("btnDeleteAll").style.display = "none";

          // Xóa dữ liệu localStorage khi giỏ hàng trống
          localStorage.removeItem("orderItems");
          clearCartState();
        }

        updateCartTotals();
        updateCartCount();

        // Re-bind events after DOM changes with a small delay to ensure DOM is updated
        setTimeout(function () {
          bindAllEvents();
        }, 50);
      } else {
        console.error("Lỗi khi xóa sản phẩm khỏi giỏ hàng");
      }
    } catch (error) {
      console.error("Lỗi khi gọi API xóa sản phẩm:", error);
    }
  }
}

// Event binding functions
function bindQuantityControls() {
  // Remove existing event listeners by replacing elements
  document.querySelectorAll(".quantity-up").forEach((btn) => {
    btn.replaceWith(btn.cloneNode(true));
  });
  document.querySelectorAll(".quantity-down").forEach((btn) => {
    btn.replaceWith(btn.cloneNode(true));
  });
  document.querySelectorAll(".quantity-input").forEach((input) => {
    input.replaceWith(input.cloneNode(true));
  });

  // Quantity up buttons
  document.querySelectorAll(".quantity-up").forEach((btn) => {
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      const rowIndex = btn.getAttribute("data-row-index");
      const input = document.querySelector(
        `.quantity-input[data-row-index="${rowIndex}"]`
      );
      let qty = parseInt(input.value, 10) || 1;
      if (qty < 99) {
        qty = qty + 1;
        input.value = qty;
        updateRowTotal(rowIndex);
      }
    });
  });

  // Quantity down buttons
  document.querySelectorAll(".quantity-down").forEach((btn) => {
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      const rowIndex = btn.getAttribute("data-row-index");
      const input = document.querySelector(
        `.quantity-input[data-row-index="${rowIndex}"]`
      );
      let qty = parseInt(input.value, 10) || 1;
      if (qty > 1) {
        qty = qty - 1;
        input.value = qty;
        updateRowTotal(rowIndex);
      }
    });
  });

  // Quantity input direct change
  document.querySelectorAll(".quantity-input").forEach((input) => {
    input.addEventListener("change", function () {
      let qty = parseInt(input.value, 10);
      if (isNaN(qty) || qty < 1) qty = 1;
      if (qty > 99) qty = 99;
      input.value = qty;

      const rowIndex = input.getAttribute("data-row-index");
      updateRowTotal(rowIndex);
    });

    // Prevent decimal input - only allow integers
    input.addEventListener("input", function () {
      // Remove any non-digit characters
      this.value = this.value.replace(/[^0-9]/g, "");

      // If empty or 0, set to 1
      if (this.value === "" || this.value === "0") {
        this.value = "1";
      }

      // Limit to 99
      let qty = parseInt(this.value, 10);
      if (qty > 99) {
        this.value = "99";
      }
    });

    // Prevent keyboard input that would create decimals or invalid characters
    input.addEventListener("keydown", function (e) {
      // Allow: backspace, delete, tab, escape, enter
      if (
        [46, 8, 9, 27, 13].indexOf(e.keyCode) !== -1 ||
        // Allow: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
        (e.keyCode === 65 && e.ctrlKey === true) ||
        (e.keyCode === 67 && e.ctrlKey === true) ||
        (e.keyCode === 86 && e.ctrlKey === true) ||
        (e.keyCode === 88 && e.ctrlKey === true) ||
        // Allow: home, end, left, right, up, down arrows
        (e.keyCode >= 35 && e.keyCode <= 40)
      ) {
        return;
      }
      // Ensure that it is a number and stop the keypress for non-numbers
      if (
        (e.shiftKey || e.keyCode < 48 || e.keyCode > 57) &&
        (e.keyCode < 96 || e.keyCode > 105)
      ) {
        e.preventDefault();
      }
    });
  });
}

function bindDeleteButtons() {
  // Remove existing event listeners by replacing elements
  document.querySelectorAll(".btn-delete").forEach((btn) => {
    btn.replaceWith(btn.cloneNode(true));
  });

  document.querySelectorAll(".btn-delete").forEach((btn) => {
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      const rowIndex = btn.getAttribute("data-row-index");
      
      // Hiển thị modal xác nhận
      const confirmModal = new bootstrap.Modal(document.getElementById('confirmDeleteModal'));
      confirmModal.show();
      
      // Xử lý khi người dùng xác nhận xóa
      const confirmBtn = document.getElementById('confirmDeleteBtn');
      confirmBtn.onclick = function() {
        confirmModal.hide();
        deleteCartRow(rowIndex);
      };
    });
  });
}

function bindSelectItems() {
  // Remove existing event listeners by replacing elements
  document.querySelectorAll(".cart-select-item").forEach((checkbox) => {
    checkbox.replaceWith(checkbox.cloneNode(true));
  });

  document.querySelectorAll(".cart-select-item").forEach((checkbox) => {
    checkbox.addEventListener("change", function () {
      updateCartTotals();
    });
  });
}

function bindSelectAll() {
  const btnSelectAll = document.getElementById("btnSelectAll");

  // Remove existing event listeners to prevent duplication
  btnSelectAll.replaceWith(btnSelectAll.cloneNode(true));
  const newBtnSelectAll = document.getElementById("btnSelectAll");

  newBtnSelectAll.addEventListener("click", function () {
    const cartRows = document.querySelectorAll(
      "#cartBodyRazor tr[data-row-index]"
    );
    const selectedCheckboxes = document.querySelectorAll(
      ".cart-select-item:checked"
    );

    if (cartRows.length === 0) return;

    // Check if all items are currently selected
    const allSelected = selectedCheckboxes.length === cartRows.length;

    // If all are selected, unselect all. Otherwise, select all
    const shouldSelect = !allSelected;

    document.querySelectorAll(".cart-select-item").forEach((checkbox) => {
      checkbox.checked = shouldSelect;
    });

    updateCartTotals();
  });
}

function bindDeleteAll() {
  const btnDeleteAll = document.getElementById("btnDeleteAll");

  // Remove existing event listeners by replacing element
  btnDeleteAll.replaceWith(btnDeleteAll.cloneNode(true));
  const newBtnDeleteAll = document.getElementById("btnDeleteAll");

  newBtnDeleteAll.addEventListener("click", async function (e) {
    e.preventDefault();
    e.stopPropagation();
    
    // Hiển thị modal xác nhận xóa tất cả
    const confirmModal = new bootstrap.Modal(document.getElementById('confirmDeleteAllModal'));
    confirmModal.show();
    
    // Xử lý khi người dùng xác nhận xóa tất cả
    const confirmBtn = document.getElementById('confirmDeleteAllBtn');
    confirmBtn.onclick = async function() {
      confirmModal.hide();
      
      // Code xóa tất cả bên dưới
      try {
        // Lấy CSRF token
        const token = document.querySelector(
          'input[name="__RequestVerificationToken"]'
        )?.value;

        // Gọi API để xóa toàn bộ giỏ hàng khỏi database
        const response = await fetch("/KhachHang/Cart/ClearCart", {
          method: "POST",
          headers: {
            "Content-Type": "application/x-www-form-urlencoded",
          },
          body: `__RequestVerificationToken=${encodeURIComponent(token || "")}`,
        });

        if (response.ok) {
          // Nếu API thành công, mới xóa khỏi DOM
          const cartBody = document.getElementById("cartBodyRazor");
          cartBody.innerHTML =
            '<tr><td colspan="6" style="text-align:center; color:#888; font-size:16px;">Không có sản phẩm nào trong giỏ hàng.</td></tr>';

          document.getElementById("btnSelectAll").style.display = "none";
          document.getElementById("btnDeleteAll").style.display = "none";

          // Xóa dữ liệu localStorage khi xóa tất cả sản phẩm
          localStorage.removeItem("orderItems");
          clearCartState();

          updateCartTotals();
          updateCartCount();
        } else {
          console.error("Lỗi khi xóa toàn bộ giỏ hàng");
        }
      } catch (error) {
        console.error("Lỗi khi gọi API xóa toàn bộ giỏ hàng:", error);
      }
    };
  });
}

function bindCheckoutButton() {
  const btnCheckout = document.getElementById("btnCheckout");
  if (btnCheckout) {
    btnCheckout.addEventListener("click", function () {
      const cartRows = document.querySelectorAll(
        "#cartBodyRazor tr[data-row-index]"
      );
      const selectedCheckboxes = document.querySelectorAll(
        ".cart-select-item:checked"
      );

      if (cartRows.length === 0) {
        const modal = new bootstrap.Modal(
          document.getElementById("emptyCartModal")
        );
        modal.show();
        return;
      }

      if (selectedCheckboxes.length === 0) {
        const modal = new bootstrap.Modal(
          document.getElementById("noSelectedModal")
        );
        modal.show();
        return;
      }

      // Collect selected items data for checkout
      const selectedItems = [];
      selectedCheckboxes.forEach((checkbox) => {
        const rowIndex = checkbox.getAttribute("data-row-index");
        const row = document.querySelector(`tr[data-row-index="${rowIndex}"]`);
        if (row) {
          const name = row.querySelector(".cart-product-name").textContent;
          const price = parseInt(
            row.querySelector(".cart-product-price").getAttribute("data-price"),
            10
          );
          const quantity = parseInt(
            row.querySelector(".quantity-input").value,
            10
          );
          const total = price * quantity;
          const idBienThe = parseInt(row.getAttribute("data-id-bien-the"), 10);

          // Lấy thông tin ảnh từ img element
          const imgElement = row.querySelector(".product-img");
          const linkAnh = imgElement
            ? imgElement.getAttribute("src")
            : "/images/noimage.jpg";

          console.log(`[Cart Debug] Product: ${name}, Image src: ${linkAnh}`);

          selectedItems.push({
            name: name,
            price: price,
            quantity: quantity,
            total: total,
            idBienThe: idBienThe,
            linkAnh: linkAnh,
          });
        }
      });

      // Store selected items for checkout (you can modify this part based on your checkout flow)
      localStorage.setItem("orderItems", JSON.stringify(selectedItems));

      // Redirect to checkout page
      window.location.href = "/KhachHang/Pay";
    });
  }
}

function bindContinueShopping() {
  const btnContinue = document.getElementById("btnContinueShopping");
  if (btnContinue) {
    btnContinue.addEventListener("click", function (e) {
      e.preventDefault();
      window.location.href = "/KhachHang/SanPham";
    });
  }
}

// Modal cleanup functions
function removeBackdrop() {
  const backdrops = document.querySelectorAll(".modal-backdrop");
  backdrops.forEach((backdrop) => {
    backdrop.remove();
  });
  document.body.classList.remove("modal-open");
  document.body.style.overflow = "";
  document.body.style.paddingRight = "";
}

function setupModalEvents() {
  const emptyCartModal = document.getElementById("emptyCartModal");
  if (emptyCartModal) {
    emptyCartModal.addEventListener("hidden.bs.modal", function () {
      setTimeout(removeBackdrop, 100);
    });
  }

  const noSelectedModal = document.getElementById("noSelectedModal");
  if (noSelectedModal) {
    noSelectedModal.addEventListener("hidden.bs.modal", function () {
      setTimeout(removeBackdrop, 100);
    });
  }

  const orderSuccessModal = document.getElementById("orderSuccessModal");
  if (orderSuccessModal) {
    orderSuccessModal.addEventListener("hidden.bs.modal", function () {
      setTimeout(removeBackdrop, 100);
    });
  }

  const confirmDeleteModal = document.getElementById("confirmDeleteModal");
  if (confirmDeleteModal) {
    confirmDeleteModal.addEventListener("hidden.bs.modal", function () {
      setTimeout(removeBackdrop, 100);
    });
  }

  const confirmDeleteAllModal = document.getElementById("confirmDeleteAllModal");
  if (confirmDeleteAllModal) {
    confirmDeleteAllModal.addEventListener("hidden.bs.modal", function () {
      setTimeout(removeBackdrop, 100);
    });
  }
}

// Cart count functions (for header)
async function updateCartCount() {
  try {
    // Gọi API để lấy số lượng chính xác từ server
    const response = await fetch("/KhachHang/Cart/GetCartCount", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.ok) {
      const result = await response.json();
      const cartCountElement = document.getElementById("cartCount");

      if (cartCountElement) {
        if (result.cartCount > 0) {
          cartCountElement.textContent = result.cartCount;
          cartCountElement.style.display = "inline";
        } else {
          cartCountElement.style.display = "none";
        }
      }
    } else {
      // Fallback: đếm từ DOM nếu API không hoạt động
      const cartRows = document.querySelectorAll(
        "#cartBodyRazor tr[data-row-index]"
      );
      let productCount = cartRows.length;

      const cartCountElement = document.getElementById("cartCount");
      if (cartCountElement) {
        cartCountElement.textContent = productCount;
        if (productCount > 0) {
          cartCountElement.style.display = "inline";
        } else {
          cartCountElement.style.display = "none";
        }
      }
    }
  } catch (error) {
    console.error("Lỗi khi cập nhật số lượng giỏ hàng:", error);

    // Fallback: đếm từ DOM nếu có lỗi
    const cartRows = document.querySelectorAll(
      "#cartBodyRazor tr[data-row-index]"
    );
    let productCount = cartRows.length;

    const cartCountElement = document.getElementById("cartCount");
    if (cartCountElement) {
      cartCountElement.textContent = productCount;
      if (productCount > 0) {
        cartCountElement.style.display = "inline";
      } else {
        cartCountElement.style.display = "none";
      }
    }
  }
}

// Make updateCartCount available globally
window.updateCartCount = updateCartCount;

// Main binding function
function bindRowClickSelect() {
  document
    .querySelectorAll("#cartBodyRazor tr[data-row-index]")
    .forEach((row) => {
      row.onclick = null;
      row.addEventListener("click", function (e) {
        // Không xử lý nếu click vào link hoặc ảnh sản phẩm
        if (e.target.closest('.product-img-link') || e.target.closest('.cart-product-name-link')) {
          return;
        }
        
        // Chỉ xử lý khi click vào chính <tr> hoặc <td> KHÔNG có thành phần con (khoảng trống)
        const isTr = e.target === row;
        const isEmptyTd =
          e.target.tagName === "TD" && e.target.children.length === 0;
        if (isTr || isEmptyTd) {
          const rowIndex = row.getAttribute("data-row-index");
          const checkbox = row.querySelector(".cart-select-item");
          if (checkbox) {
            checkbox.checked = !checkbox.checked;
            updateCartTotals();
          }
        }
      });
    });
}

// Product variant functions
function loadProductVariants(rowIndex) {
  const container = document.querySelector(
    `.product-variants-container[data-row-index="${rowIndex}"]`
  );
  if (!container) {
    console.log(`[Variant] No container found for row ${rowIndex}`);
    return;
  }

  const productId = container.getAttribute("data-id-san-pham");
  const currentBienTheId = container.getAttribute("data-id-bien-the");

  console.log(`[Variant] Loading variants for product ${productId}, current variant: ${currentBienTheId}`);

  fetch(`/KhachHang/Cart/GetProductVariants?productId=${productId}`)
    .then((response) => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then((data) => {
      console.log('[Variant] API Response:', data);
      
      if (data.success && data.variants && data.variants.length > 0) {
        // ASP.NET Core serialize JSON thành camelCase, cần normalize về PascalCase
        const normalizedVariants = data.variants.map(v => ({
          IdBienThe: v.idBienThe || v.IdBienThe,
          Sku: v.sku || v.Sku,
          GiaBan: v.giaBan || v.GiaBan,
          SoLuongTonKho: v.soLuongTonKho || v.SoLuongTonKho,
          ThuocTinhs: (v.thuocTinhs || v.ThuocTinhs || []).map(t => ({
            TenThuocTinh: t.tenThuocTinh || t.TenThuocTinh,
            GiaTri: t.giaTri || t.GiaTri
          }))
        }));
        
        console.log('[Variant] Variants received:', normalizedVariants.map(v => ({
          id: v.IdBienThe,
          sku: v.Sku,
          attrs: v.ThuocTinhs ? v.ThuocTinhs.length : 0
        })));
        
        updateVariantSelectors(rowIndex, normalizedVariants, currentBienTheId);
      } else {
        console.warn('[Variant] No variants found or API returned error:', data);
      }
    })
    .catch((error) => {
      console.error("Error loading variants:", error);
    });
}

function updateVariantSelectors(rowIndex, variants, currentBienTheId) {
  const container = document.querySelector(
    `.product-variants-container[data-row-index="${rowIndex}"]`
  );
  if (!container) {
    console.warn(`[Variant] Container not found for row ${rowIndex}`);
    return;
  }

  console.log(`[Variant] Updating selectors for row ${rowIndex}`, {
    totalVariants: variants.length,
    currentBienTheId,
    currentBienTheIdType: typeof currentBienTheId
  });

  // DEBUG: In ra chi tiết tất cả variant IDs
  console.log('[Variant] All variants with full details:');
  variants.forEach((v, idx) => {
    console.log(`  ${idx + 1}. ID=${v.IdBienThe} (type: ${typeof v.IdBienThe}), SKU=${v.Sku}, Attrs=${v.ThuocTinhs?.length || 0}`);
  });

  // Find current variant - QUAN TRỌNG: So sánh đúng kiểu
  const currentId = parseInt(currentBienTheId);
  console.log('[Variant] Looking for variant with ID:', currentId, 'type:', typeof currentId);
  
  const currentVariant = variants.find(v => {
    const vId = parseInt(v.IdBienThe);
    const match = vId === currentId;
    console.log(`  Checking variant ${v.IdBienThe}: ${vId} === ${currentId}? ${match}`);
    return match;
  });
  
  if (!currentVariant) {
    console.error('[Variant] ❌ Current variant not found!', {
      lookingFor: currentId,
      lookingForType: typeof currentId,
      availableIds: variants.map(v => v.IdBienThe),
      availableTypes: variants.map(v => typeof v.IdBienThe)
    });
    return;
  }

  console.log('[Variant] ✅ Current variant found:', currentVariant);

  // Group ALL unique attribute names and values from ALL variants
  const allAttributes = {};
  variants.forEach((variant) => {
    if (variant.ThuocTinhs && Array.isArray(variant.ThuocTinhs)) {
      variant.ThuocTinhs.forEach((thuocTinh) => {
        const attrName = thuocTinh.TenThuocTinh;
        const attrValue = thuocTinh.GiaTri;
        
        if (attrName && attrValue) {
          if (!allAttributes[attrName]) {
            allAttributes[attrName] = new Set();
          }
          allAttributes[attrName].add(attrValue);
        }
      });
    }
  });

  console.log('[Variant] All unique attributes:', Object.keys(allAttributes));

  // Store variants data
  container.setAttribute("data-variants", JSON.stringify(variants));

  // Get current selected values from current variant
  const currentValues = {};
  if (currentVariant.ThuocTinhs && Array.isArray(currentVariant.ThuocTinhs)) {
    currentVariant.ThuocTinhs.forEach((thuocTinh) => {
      currentValues[thuocTinh.TenThuocTinh] = thuocTinh.GiaTri;
    });
  }

  console.log('[Variant] Current selected values:', currentValues);

  // Check which attributes are already in DOM
  const existingDropdowns = container.querySelectorAll(".variant-dropdown");
  const existingAttrs = new Set();
  existingDropdowns.forEach(dropdown => {
    const attrName = dropdown.getAttribute("data-thuoc-tinh");
    if (attrName) {
      existingAttrs.add(attrName);
    }
  });

  console.log('[Variant] Existing attributes in DOM:', Array.from(existingAttrs));

  // ADD MISSING ATTRIBUTES TO DOM
  Object.keys(allAttributes).forEach((attrName) => {
    if (!existingAttrs.has(attrName)) {
      console.log(`[Variant] ➕ Adding missing attribute: ${attrName}`);
      
      // Create new variant item
      const itemDiv = document.createElement("div");
      itemDiv.className = "variant-item";
      
      const displaySpan = document.createElement("span");
      displaySpan.className = "variant-display";
      
      const nameSpan = document.createElement("span");
      nameSpan.className = "variant-name";
      nameSpan.textContent = attrName + ":";
      
      const valueSpan = document.createElement("span");
      valueSpan.className = "variant-value";
      valueSpan.textContent = currentValues[attrName] || "";
      
      const arrow = document.createElement("i");
      arrow.className = "fas fa-chevron-down variant-arrow";
      
      displaySpan.appendChild(nameSpan);
      displaySpan.appendChild(valueSpan);
      displaySpan.appendChild(arrow);
      
      const dropdown = document.createElement("div");
      dropdown.className = "variant-dropdown";
      dropdown.setAttribute("data-thuoc-tinh", attrName);
      dropdown.setAttribute("data-row-index", rowIndex);
      
      itemDiv.appendChild(displaySpan);
      itemDiv.appendChild(dropdown);
      container.appendChild(itemDiv);
      
      existingAttrs.add(attrName);
    }
  });

  // NOW UPDATE ALL DROPDOWNS WITH ALL OPTIONS
  const allItems = container.querySelectorAll(".variant-item");
  allItems.forEach((item) => {
    const dropdown = item.querySelector(".variant-dropdown");
    const valueSpan = item.querySelector(".variant-value");
    const attrName = dropdown.getAttribute("data-thuoc-tinh");
    
    if (allAttributes[attrName]) {
      const currentValue = currentValues[attrName] || valueSpan.textContent;
      
      // Clear and rebuild options
      dropdown.innerHTML = "";
      
      const sortedValues = Array.from(allAttributes[attrName]).sort();
      sortedValues.forEach((value) => {
        const optionDiv = document.createElement("div");
        optionDiv.className = "variant-option";
        optionDiv.setAttribute("data-value", value);
        optionDiv.textContent = value;
        
        if (value === currentValue) {
          optionDiv.classList.add("selected");
        }
        
        dropdown.appendChild(optionDiv);
      });
      
      // Update display text
      valueSpan.textContent = currentValue;
      
      console.log(`[Variant] ✓ Updated "${attrName}" with ${sortedValues.length} options:`, sortedValues);
    }
  });

  console.log('[Variant] ✅ All variant selectors updated successfully');
}

function handleVariantChange(rowIndex) {
  const container = document.querySelector(
    `.product-variants-container[data-row-index="${rowIndex}"]`
  );
  if (!container) return;

  const variants = JSON.parse(container.getAttribute("data-variants") || "[]");
  
  // Get selected values from variant-value spans (current displayed values)
  const selectedValues = {};
  const variantItems = container.querySelectorAll(".variant-item");
  
  variantItems.forEach((item) => {
    const attrName = item.getAttribute("data-attr-name");
    const valueSpan = item.querySelector(".variant-value");
    if (attrName && valueSpan) {
      selectedValues[attrName] = valueSpan.textContent.trim();
    }
  });

  console.log('[handleVariantChange] Row:', rowIndex, 'Selected values:', selectedValues);

  // Find matching variant
  const matchingVariant = variants.find((variant) => {
    const match = variant.ThuocTinhs.every(
      (thuocTinh) => selectedValues[thuocTinh.TenThuocTinh] === thuocTinh.GiaTri
    );
    return match;
  });

  console.log('[handleVariantChange] Matching variant:', matchingVariant);

  if (matchingVariant) {
    const oldBienTheId = container.getAttribute("data-id-bien-the");
    const newBienTheId = matchingVariant.IdBienThe;

    console.log('[handleVariantChange] Old ID:', oldBienTheId, 'New ID:', newBienTheId);

    if (oldBienTheId != newBienTheId) {
      updateCartVariant(rowIndex, oldBienTheId, newBienTheId);
    } else {
      console.log('[handleVariantChange] Same variant, no update needed');
    }
  } else {
    console.log('[handleVariantChange] No matching variant found!');
  }
}

async function updateCartVariant(rowIndex, oldBienTheId, newBienTheId) {
  const container = document.querySelector(
    `.product-variants-container[data-row-index="${rowIndex}"]`
  );
  if (!container) return;

  const row = document.querySelector(`tr[data-row-index="${rowIndex}"]`);
  
  console.log('[updateCartVariant] Updating variant...', { rowIndex, oldBienTheId, newBienTheId });

  try {
    const response = await fetch("/KhachHang/Cart/UpdateCartVariant", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken:
          document.querySelector('input[name="__RequestVerificationToken"]')
            ?.value || "",
      },
      body: JSON.stringify({
        OldBienTheId: parseInt(oldBienTheId),
        NewBienTheId: parseInt(newBienTheId),
      }),
    });

    const data = await response.json();
    console.log('[updateCartVariant] Response:', data);

    if (data.success) {
      // Update container data
      container.setAttribute("data-id-bien-the", newBienTheId);

      // Update row data
      if (row) {
        row.setAttribute("data-id-bien-the", newBienTheId);
      }

      // ===== CẬP NHẬT GIÁ =====
      const priceElement = row?.querySelector(".cart-product-price");
      const totalElement = row?.querySelector(".cart-product-total");
      const quantityInput = row?.querySelector(".quantity-input");
      
      if (priceElement && data.newPrice !== undefined) {
        const newPrice = parseFloat(data.newPrice);
        console.log('[updateCartVariant] Updating price from', priceElement.getAttribute("data-price"), 'to', newPrice);
        
        // Cập nhật giá đơn vị
        priceElement.setAttribute("data-price", newPrice);
        priceElement.textContent = formatVND(newPrice);
        
        // Cập nhật tổng tiền
        if (totalElement && quantityInput) {
          const quantity = parseInt(quantityInput.value) || 1;
          const total = newPrice * quantity;
          totalElement.textContent = formatVND(total);
          totalElement.setAttribute("data-price", newPrice); // Lưu giá đơn vị vào data-price
          
          console.log('[updateCartVariant] Updated total:', { quantity, newPrice, total });
        }
      }

      // Update image
      const imgElement = document.querySelector(
        `img.product-img[data-row-index="${rowIndex}"]`
      );
      if (imgElement && data.newImage) {
        console.log('[updateCartVariant] Updating image from', imgElement.src, 'to', data.newImage);
        
        // Kiểm tra nếu là URL đầy đủ (http/https) thì giữ nguyên
        if (data.newImage.startsWith('http://') || data.newImage.startsWith('https://')) {
          imgElement.src = data.newImage;
        } else if (data.newImage.startsWith('/')) {
          imgElement.src = data.newImage;
        } else {
          imgElement.src = '/' + data.newImage;
        }
        
        // Thêm error handler
        imgElement.onerror = function() {
          console.error('[updateCartVariant] Failed to load image:', data.newImage);
          this.onerror = null;
          this.src = '/images/noimage.jpg';
        };
      }

      // Recalculate cart totals
      updateCartTotals();
      
      // Update cart count in header
      updateCartCount();

      // Show success notification - REMOVED
      // showVariantChangeNotification("✓ Đã cập nhật thuộc tính và giá sản phẩm");
      
      console.log('[updateCartVariant] ✅ Update successful!');
    } else {
      // showVariantChangeNotification("❌ Không thể cập nhật thuộc tính", "error");
      console.error('[updateCartVariant] Update failed:', data.message);
    }
  } catch (error) {
    console.error('[updateCartVariant] Error:', error);
    // showVariantChangeNotification("❌ Có lỗi xảy ra", "error");
  }
}

function showVariantChangeNotification(message, type = "success") {
  const notification = document.createElement("div");
  notification.className = `variant-notification ${type}`;
  notification.innerHTML = `
    <i class="fas ${type === "success" ? "fa-check-circle" : "fa-exclamation-circle"}"></i>
    <span>${message}</span>
  `;

  document.body.appendChild(notification);

  setTimeout(() => {
    notification.classList.add("show");
  }, 100);

  setTimeout(() => {
    notification.classList.remove("show");
    setTimeout(() => {
      notification.remove();
    }, 300);
  }, 2000);
}

function bindVariantSelectors() {
  console.log('[Variant] PORTAL TECHNIQUE - GUARANTEED TO WORK...');
  
  // Create dropdown portal container
  let portal = document.getElementById('dropdown-portal');
  if (!portal) {
    portal = document.createElement('div');
    portal.id = 'dropdown-portal';
    portal.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
      z-index: 999999999;
    `;
    document.body.appendChild(portal);
  }
  
  // ONE SIMPLE EVENT LISTENER
  document.addEventListener('click', function(e) {
    // Handle dropdown toggle
    if (e.target.closest('.variant-display')) {
      e.preventDefault();
      const display = e.target.closest('.variant-display');
      const item = display.closest('.variant-item');
      const originalDropdown = item.querySelector('.variant-dropdown');
      
      console.log('[Dropdown] Display clicked!', { display, item, originalDropdown });
      
      // Close all active dropdowns
      document.querySelectorAll('.variant-item.active').forEach(other => {
        other.classList.remove('active');
      });
      portal.innerHTML = ''; // Clear portal
      
      // If clicking same item, just close
      if (item.classList.contains('active')) {
        item.classList.remove('active');
        return;
      }
      
      // Open new dropdown
      item.classList.add('active');
      
      // Clone dropdown and put in portal
      const portalDropdown = originalDropdown.cloneNode(true);
      portalDropdown.style.cssText = `
        position: fixed;
        display: block !important;
        background: white;
        border: 2px solid #4caf50;
        border-radius: 8px;
        box-shadow: 0 4px 20px rgba(0,0,0,0.2);
        z-index: 999999999;
        min-width: 200px;
        pointer-events: auto;
      `;
      
      // Position dropdown
      const rect = display.getBoundingClientRect();
      portalDropdown.style.top = (rect.bottom + 4) + 'px';
      portalDropdown.style.left = rect.left + 'px';
      
      // Add to portal
      portal.appendChild(portalDropdown);
      
      console.log('[Dropdown] Portal dropdown created at:', { 
        top: rect.bottom + 4, 
        left: rect.left,
        portalDropdown 
      });
      
      return;
    }
    
    // Handle option selection - Works with portal
    if (e.target.closest('.variant-option')) {
      e.preventDefault();
      const option = e.target.closest('.variant-option');
      const selectedValue = option.getAttribute('data-value');
      
      console.log('[Option] Clicked:', selectedValue, option);
      
      // Find the active variant item in the main document  
      const activeItem = document.querySelector('.variant-item.active');
      if (activeItem) {
        const valueSpan = activeItem.querySelector('.variant-value');
        const originalDropdown = activeItem.querySelector('.variant-dropdown');
        
        // Update display text
        if (valueSpan) {
          valueSpan.textContent = selectedValue;
        }
        
        // Update selected state in original dropdown
        if (originalDropdown) {
          originalDropdown.querySelectorAll('.variant-option').forEach(opt => opt.classList.remove('selected'));
          const matchingOption = [...originalDropdown.querySelectorAll('.variant-option')]
            .find(opt => opt.getAttribute('data-value') === selectedValue);
          if (matchingOption) {
            matchingOption.classList.add('selected');
          }
        }
        
        // Close dropdown
        activeItem.classList.remove('active');
        const portal = document.getElementById('dropdown-portal');
        if (portal) portal.innerHTML = '';
        
        // Handle variant change
        const rowIndex = originalDropdown ? originalDropdown.getAttribute('data-row-index') : null;
        if (rowIndex) {
          handleVariantChange(rowIndex);
        }
        
        console.log('[Option] Selected:', selectedValue, 'for row:', rowIndex);
      }
      return;
    }
    
    // Close all when clicking outside
    if (!e.target.closest('.variant-item') && !e.target.closest('#dropdown-portal')) {
      document.querySelectorAll('.variant-item.active').forEach(item => {
        item.classList.remove('active');
      });
      const portal = document.getElementById('dropdown-portal');
      if (portal) portal.innerHTML = '';
    }
  });
  
  // Handle scroll - reposition dropdown
  window.addEventListener('scroll', function() {
    const portal = document.getElementById('dropdown-portal');
    const activeItem = document.querySelector('.variant-item.active');
    
    if (portal && activeItem) {
      const display = activeItem.querySelector('.variant-display');
      const portalDropdown = portal.querySelector('.variant-dropdown');
      
      if (display && portalDropdown) {
        const rect = display.getBoundingClientRect();
        
        // Check if display is still visible
        if (rect.top < 0 || rect.bottom > window.innerHeight) {
          // Display is out of viewport - close dropdown
          activeItem.classList.remove('active');
          portal.innerHTML = '';
        } else {
          // Reposition dropdown
          portalDropdown.style.top = (rect.bottom + 4) + 'px';
          portalDropdown.style.left = rect.left + 'px';
        }
      }
    }
  }, { passive: true });
  
  // Handle resize - close dropdown
  window.addEventListener('resize', function() {
    const portal = document.getElementById('dropdown-portal');
    const activeItems = document.querySelectorAll('.variant-item.active');
    
    activeItems.forEach(item => item.classList.remove('active'));
    if (portal) portal.innerHTML = '';
  });
  
  // Load variants for each product
  document.querySelectorAll(".product-variants-container").forEach((container) => {
    const rowIndex = container.getAttribute("data-row-index");
    if (rowIndex) {
      loadProductVariants(rowIndex);
    }
  });
}



function bindAllEvents() {
  bindQuantityControls();
  bindDeleteButtons();
  bindSelectItems();
  bindSelectAll();
  bindDeleteAll();
  bindCheckoutButton();
  bindContinueShopping();
  setupModalEvents();
  bindRowClickSelect();
  bindVariantSelectors();
}

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  // Kiểm tra nếu có query parameter refresh=true thì clear localStorage
  const urlParams = new URLSearchParams(window.location.search);
  if (urlParams.get("refresh") === "true") {
    console.log("Refresh giỏ hàng sau thanh toán thành công");
    localStorage.removeItem("orderItems");
    localStorage.removeItem("cartSelected");
    localStorage.removeItem("selectedCartItems");
    clearCartState();

    // Remove query parameter khỏi URL
    window.history.replaceState({}, document.title, window.location.pathname);
  }

  // Wait a bit for DOM to be fully rendered
  setTimeout(function () {
    // Load saved cart state first (restore selected checkboxes)
    loadCartState();
    
    // Then update totals and bind events
    updateCartTotals();
    updateCartCount();
    bindAllEvents();

    // Debug: Log the number of cart items found
    const cartRows = document.querySelectorAll(
      "#cartBodyRazor tr[data-row-index]"
    );
    console.log(`Found ${cartRows.length} cart items in DOM`);
  }, 100);
});
function viewOrderDetail(orderId) {
  if (!orderId) {
    showNotification("Lỗi: Không tìm thấy ID đơn hàng", "error");
    return;
  }

  // Show loading in modal
  const modal = new bootstrap.Modal(
    document.getElementById("orderDetailModal")
  );
  const modalContent = document.getElementById("orderDetailContent");

  modalContent.innerHTML = `
        <div class="text-center p-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Đang tải...</span>
            </div>
            <p class="mt-2">Đang tải chi tiết đơn hàng...</p>
        </div>
    `;

  modal.show();

  // Fetch order details
  fetch(`/KhachHang/DonHang/GetOrderDetail/${orderId}`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.text();
    })
    .then((html) => {
      modalContent.innerHTML = html;
    })
    .catch((error) => {
      console.error("Error:", error);
      modalContent.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Có lỗi xảy ra khi tải chi tiết đơn hàng. Vui lòng thử lại.
            </div>
        `;
      showNotification("Lỗi khi tải chi tiết đơn hàng", "error");
    });
}

// Cancel order function
function cancelOrder(orderId) {
  if (confirm("Bạn có chắc chắn muốn hủy đơn hàng này?")) {
    fetch("/KhachHang/DonHang/CancelOrder", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ id: orderId }),
    })
      .then((response) => response.json())
      .then((data) => {
        if (data.success) {
          showNotification(data.message, "success");
          setTimeout(() => {
            location.reload();
          }, 1500);
        } else {
          showNotification(data.message, "error");
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        showNotification("Có lỗi xảy ra khi hủy đơn hàng", "error");
      });
  }
}

// Notification function
function showNotification(message, type = "info", duration = 3000) {
  const container = document.getElementById("notificationContainer");
  if (!container) return;

  const notification = document.createElement("div");
  notification.className = `alert alert-${
    type === "error" ? "danger" : type
  } alert-dismissible fade show position-fixed`;
  notification.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    `;

  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-${
              type === "success"
                ? "check-circle"
                : type === "error"
                ? "exclamation-triangle"
                : "info-circle"
            } me-2"></i>
            <span>${message}</span>
            <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert"></button>
        </div>
    `;

  container.appendChild(notification);

  setTimeout(() => {
    if (notification.parentNode) {
      notification.remove();
    }
  }, duration);
}

function viewOrderDetail(orderId) {
  if (!orderId) {
    showNotification("Lỗi: Không tìm thấy ID đơn hàng", "error");
    return;
  }

  // Show loading in modal
  const modal = new bootstrap.Modal(
    document.getElementById("orderDetailModal")
  );
  const modalContent = document.getElementById("orderDetailContent");

  modalContent.innerHTML = `
        <div class="text-center p-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Đang tải...</span>
            </div>
            <p class="mt-2">Đang tải chi tiết đơn hàng...</p>
        </div>
    `;

  modal.show();

  // Fetch order details
  fetch(`/KhachHang/DonHang/GetOrderDetail/${orderId}`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.text();
    })
    .then((html) => {
      modalContent.innerHTML = html;
    })
    .catch((error) => {
      console.error("Error:", error);
      modalContent.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Có lỗi xảy ra khi tải chi tiết đơn hàng. Vui lòng thử lại.
            </div>
        `;
      showNotification("Lỗi khi tải chi tiết đơn hàng", "error");
    });
}

// Cancel order function
function cancelOrder(orderId) {
  if (confirm("Bạn có chắc chắn muốn hủy đơn hàng này?")) {
    fetch("/KhachHang/DonHang/CancelOrder", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ id: orderId }),
    })
      .then((response) => response.json())
      .then((data) => {
        if (data.success) {
          showNotification(data.message, "success");
          setTimeout(() => {
            location.reload();
          }, 1500);
        } else {
          showNotification(data.message, "error");
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        showNotification("Có lỗi xảy ra khi hủy đơn hàng", "error");
      });
  }
}

// Notification function
function showNotification(message, type = "info", duration = 3000) {
  const container = document.getElementById("notificationContainer");
  if (!container) return;

  const notification = document.createElement("div");
  notification.className = `alert alert-${
    type === "error" ? "danger" : type
  } alert-dismissible fade show position-fixed`;
  notification.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    `;

  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-${
              type === "success"
                ? "check-circle"
                : type === "error"
                ? "exclamation-triangle"
                : "info-circle"
            } me-2"></i>
            <span>${message}</span>
            <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert"></button>
        </div>
    `;

  container.appendChild(notification);

  setTimeout(() => {
    if (notification.parentNode) {
      notification.remove();
    }
  }, duration);
}
