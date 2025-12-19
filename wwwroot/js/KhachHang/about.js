document.addEventListener("DOMContentLoaded", function () {
  const authorCards = document.querySelectorAll(".author-card");
  const authorsGrid = document.querySelector(".authors-grid");

  authorCards.forEach((card) => {
    card.addEventListener("mouseenter", function () {
      // Thêm class để làm mờ nền
      authorsGrid.classList.add("blur-background");
      this.classList.add("active");
      this.style.filter = "brightness(1.1)";
    });

    card.addEventListener("mouseleave", function () {
      // Bỏ class làm mờ nền
      authorsGrid.classList.remove("blur-background");
      this.classList.remove("active");
      this.style.filter = "brightness(1)";
    });

    // Thêm hiệu ứng click (tùy chọn)
    card.addEventListener("click", function () {
      const authorName = this.querySelector(".author-name").textContent;
      console.log(`Clicked on ${authorName}`);
      // Có thể thêm modal hoặc redirect đến trang chi tiết tác giả
    });
  });

  // Hiệu ứng xuất hiện từ từ khi trang load
  const observerOptions = {
    threshold: 0.1,
    rootMargin: "0px 0px -50px 0px",
  };

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry, index) => {
      if (entry.isIntersecting) {
        setTimeout(() => {
          entry.target.style.opacity = "1";
          entry.target.style.transform = "translateY(0)";
        }, index * 100);
      }
    });
  }, observerOptions);

  authorCards.forEach((card) => {
    card.style.opacity = "0";
    card.style.transform = "translateY(30px)";
    card.style.transition = "opacity 0.6s ease, transform 0.6s ease";
    observer.observe(card);
  });
});
