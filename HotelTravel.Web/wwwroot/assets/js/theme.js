(function ($) {
  'use strict';

  //Header Search
  if ($('.search-box-outer').length) {
    $('.search-box-outer').on('click', function () {
      $('body').addClass('search-active');
    });
    $('.close-search').on('click', function () {
      $('body').removeClass('search-active');
    });
  }

  // Mobile Menu
  $('.mobile-menu nav').meanmenu({
    meanScreenWidth: '991',
    meanMenuContainer: '.mobile-menu',
    meanMenuOpen: '<span></span> <span></span> <span></span>',
    onePage: false,
  });

  // sticky
  var wind = $(window);
  var sticky = $('#sticky-header');
  wind.on('scroll', function () {
    var scroll = wind.scrollTop();
    if (scroll < 100) {
      sticky.removeClass('sticky');
    } else {
      sticky.addClass('sticky');
    }
  });

  // Loder  //
  $(function () {
    $('body').addClass('loaded');
  });

  // Sidebar

  ('use strict');
  jQuery(document).ready(function (o) {
    0 < o('.offset-side-bar').length &&
      o('.offset-side-bar').on('click', function (e) {
        e.preventDefault(),
          e.stopPropagation(),
          o('.cart-group').addClass('isActive');
      }),
      0 < o('.close-side-widget').length &&
        o('.close-side-widget').on('click', function (e) {
          e.preventDefault(), o('.cart-group').removeClass('isActive');
        }),
      0 < o('.navSidebar-button').length &&
        o('.navSidebar-button').on('click', function (e) {
          e.preventDefault(),
            e.stopPropagation(),
            o('.info-group').addClass('isActive');
        }),
      0 < o('.close-side-widget').length &&
        o('.close-side-widget').on('click', function (e) {
          e.preventDefault(), o('.info-group').removeClass('isActive');
        }),
      o('body').on('click', function (e) {
        o('.info-group').removeClass('isActive'),
          o('.cart-group').removeClass('isActive');
      }),
      o('.xs-sidebar-widget').on('click', function (e) {
        e.stopPropagation();
      }),
      0 < o('.xs-modal-popup').length &&
        o('.xs-modal-popup').magnificPopup({
          type: 'inline',
          fixedContentPos: !2,
          fixedBgPos: !0,
          overflowY: 'auto',
          closeBtnInside: !2,
          callbacks: {
            beforeOpen: function () {
              this.st.mainClass = 'my-mfp-slide-bottom xs-promo-popup';
            },
          },
        });
  });

  ///////////////////////////////
  $(document).ready(function () {
    $('.odometer-wrapper').appear(function () {
      let count = $(this).attr('data-count');
      let odometer = $(this).closest('.odometer-wrapper').find('.odometer');

      setTimeout(function () {
        odometer.html(count);
      }, 500);
    });
  });

     

  // Slider banner

  document.addEventListener('DOMContentLoaded', function () {
    var swiper = new Swiper('.slider-banner', {
      grabCursor: true,
      autoplay: {
        speed: 1500,
        delay: 2000,
        pauseOnMouseEnter: true,
      },
      navigation: {
        nextEl: '.sliders-next',
        prevEl: '.sliders-prev',
      },
    });
  });

  // Slider banner 4
  var swiper = new Swiper(".banner_4", {
      effect: "coverflow",
      loop: true, 
      // autoplay: {
      //   delay: 5000,
      //   disableOnInteraction: false,
      // },
      navigation: {
        nextEl: '.slider-prev',
        prevEl: '.slider-next',
      },
    });

    // banner slider 5
    var swiper = new Swiper(".banner_5", {
      loop: true, // Ensures the slider loops continuously
      autoplay: {
        speed: 3000,
        delay: 5000, // Adjust the delay (time between slides in milliseconds)
        disableOnInteraction: false, // Keeps autoplay even after user interaction
      },
      effect: "fade",
      navigation: {
        nextEl: '.slider-prev',
        prevEl: '.slider-next',
      },
    });


  // Service list
  var swiper = new Swiper('.dine_list', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    grabCursor: true,
    spaceBetween: 30,
    loop: true,
    autoplay: {
      speed: 2500,
      delay: 3000,
    },
    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 1,
      },
      992: {
        slidesPerView: 3,
      },
      1400: {
        slidesPerView: 3,
      },

      1920: {
        slidesPerView: 3,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
    navigation: {
      nextEl: '.slider-next2',
      prevEl: '.slider-prev2',
    },
  });

  // Service list
  var swiper = new Swiper('.dine_list2', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    grabCursor: true,
    spaceBetween: 30,
    loop: true,
    autoplay: {
      speed: 2500,
      delay: 3000,
    },
    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 1,
      },
      992: {
        slidesPerView: 3,
      },
      1400: {
        slidesPerView: 4,
      },

      1920: {
        slidesPerView: 4,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
    navigation: {
      nextEl: '.slider-next2',
      prevEl: '.slider-prev2',
    },
  });

  // Galary list
  var swiper = new Swiper('.galary_list', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    grabCursor: false,
    spaceBetween: 0,
    loop: true,
    autoplay: false,

    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      480: {
        slidesPerView: 2,
      },
      600: {
        slidesPerView: 2,
      },
      768: {
        slidesPerView: 3,
      },
      992: {
        slidesPerView: 5,
      },
      1400: {
        slidesPerView: 6,
      },

      1920: {
        slidesPerView: 8,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
  });

  // Galary list
  var swiper = new Swiper('.galary_list2', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    grabCursor: false,
    spaceBetween: 0,
    loop: true,
    autoplay: false,

    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 2,
      },
      768: {
        slidesPerView: 3,
      },
      992: {
        slidesPerView: 5,
      },
      1400: {
        slidesPerView: 4,
      },

      1920: {
        slidesPerView: 6,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
  });

  // Service list2
  var swiper = new Swiper('.service_list2', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    grabCursor: true,
    spaceBetween: 30,
    loop: true,
    autoplay: {
      speed: 2500,
      delay: 3000,
    },
    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 1,
      },
      992: {
        slidesPerView: 2,
      },
      1400: {
        slidesPerView: 2,
      },

      1920: {
        slidesPerView: 2,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
    navigation: {
      nextEl: '.slider-next2',
      prevEl: '.slider-prev2',
    },
  });

// choose slider 2
  var swiper = new Swiper('.choose-slider2', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    grabCursor: true,
    spaceBetween: 30,
    loop: true,
    autoplay: {
      speed: 2500,
      delay: 3000,
    },
    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 1,
      },
      992: {
        slidesPerView: 1,
      },
      1400: {
        slidesPerView: 1,
      },

      1920: {
        slidesPerView: 1,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
    pagination: {
      el: '.hotelhub-slider-dots4',
      clickable: true,
    },
  });

  // Testi slider 1
  document.addEventListener('DOMContentLoaded', function () {
    var swiper = new Swiper('.testi_list1', {
      slidesPerView: 5,
      centeredSlides: false,
      slidesPerGroup: 1,
      loop: true,
      grabCursor: true,
      spaceBetween: 25,
      keyboard: {
        enabled: true,
      },
      breakpoints: {
        0: {
          slidesPerView: 1,
        },
        600: {
          slidesPerView: 1,
        },
        768: {
          slidesPerView: 2, // Smooth transition
        },
        992: {
          slidesPerView: 3,
        },
        1400: {
          slidesPerView: 4,
        },
        1920: {
          slidesPerView: 5,
        },
      },
      pagination: {
        el: '.hotelhub-slider-dots3',
        clickable: true,
      },
    });
  });

  // Testi list 2
  var swiper = new Swiper('.testi_list2', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    loop: true,
    center: true,
    grabCursor: true,
    spaceBetween: 25,
    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 1,
      },
      992: {
        slidesPerView: 1,
      },
      1400: {
        slidesPerView: 2,
      },

      1920: {
        slidesPerView: 2,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
    pagination: {
      el: '.hotelhub-slider-dots5',
      clickable: true,
    },
  });

  // Testi list 3
  var swiper = new Swiper('.testi_list3', {
    slidesPerView: 5,
    centeredSlides: true,
    slidesPerGroupSkip: 5,
    loop: true,
    grabCursor: true,
    spaceBetween: 25,
    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 1,
      },
      992: {
        slidesPerView: 3,
      },
      1400: {
        slidesPerView: 3,
      },

      1920: {
        slidesPerView: 3,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
    navigation: {
      nextEl: '.slider-next',
      prevEl: '.slider-prev',
    },
    pagination: {
      el: '.tp-slider-dots',
      clickable: true,
    },
  });

  // Choose slider
  document.addEventListener('DOMContentLoaded', function () {
    var swiper = new Swiper('.choose-slider', {
      slidesPerView: 5,
      centeredSlides: false,
      slidesPerGroupSkip: 5,
      loop: true,
      grabCursor: true,
      spaceBetween: 30,
      keyboard: {
        enabled: true,
      },
      breakpoints: {
        0: {
          slidesPerView: 1,
        },
        600: {
          slidesPerView: 1,
        },
        768: {
          slidesPerView: 2,
        },
        992: {
          slidesPerView: 3,
        },
        1400: {
          slidesPerView: 4,
        },
        1920: {
          slidesPerView: 4,
        },
      },
      pagination: {
        el: '.hotelhub-slider-dots',
        clickable: true,
      },
    });
  });

  // Service list
  document.addEventListener('DOMContentLoaded', function () {
    var swiper = new Swiper('.swiper.service_list', {
      slidesPerView: 5,
      centeredSlides: false,
      slidesPerGroupSkip: 5,
      loop: true,
      center: true,
      grabCursor: true,
      spaceBetween: 25,
      keyboard: {
        enabled: true,
      },
      breakpoints: {
        0: {
          slidesPerView: 1,
        },
        768: {
          slidesPerView: 2,
        },
        992: {
          slidesPerView: 3,
        },
        1400: {
          slidesPerView: 3,
        },
        1920: {
          slidesPerView: 3,
        },
      },
      scrollbar: {
        el: '.swiper-scrollbar',
      },
      pagination: {
        el: '.hotelhub-slider-dots2',
        clickable: true,
      },
    });
  });

  // Choose slider
  var swiper = new Swiper('.list-slider', {
    slidesPerView: 5,
    centeredSlides: false,
    slidesPerGroupSkip: 5,
    loop: true,
    grabCursor: true,
    spaceBetween: 30,
    keyboard: {
      enabled: true,
    },
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      600: {
        slidesPerView: 2,
      },
      992: {
        slidesPerView: 3,
      },
      1400: {
        slidesPerView: 4,
      },

      1920: {
        slidesPerView: 4,
      },
    },
    scrollbar: {
      el: '.swiper-scrollbar',
    },
    navigation: {
      nextEl: '.swiper-button-next',
      prevEl: '.swiper-button-prev',
    },
  });

  // my swiper6

  var swiper = new Swiper('.mySwiper6', {
    // effect: "cards",
    grabCursor: true,
    autoplay: {
      speed: 1500,
      delay: 2000,
      pauseOnMouseEnter: true,
    },
  });

  // tsti
  var swiper = new Swiper('.testi', {
    grabCursor: true,
    effect: 'creative',
    creativeEffect: {
      prev: {
        shadow: true,
        translate: [0, 0, -400],
      },
      next: {
        translate: ['100%', 0, 0],
      },
    },
  });

  // testi 4
  var swiper = new Swiper('.testi_4', {
    // effect: "cards",
    grabCursor: true,
    autoplay: {
      speed: 1500,
      delay: 2000,
      pauseOnMouseEnter: true,
    },
    navigation: {
      nextEl: '.slider-prev',
      prevEl: '.slider-next',
    },
  });

  // Brand Slide
  var swiper = new Swiper('.brand', {
    spaceBetween: 30,
    freeMode: true,
    breakpoints: {
      0: {
        slidesPerView: 1,
      },
      480: {
        slidesPerView: 2,
      },
      600: {
        slidesPerView: 2,
      },
      768: {
        slidesPerView: 3,
      },
      992: {
        slidesPerView: 4,
      },
      1400: {
        slidesPerView: 6,
      },
      1920: {
        slidesPerView: 6,
      },
    },

    navigation: {
      nextEl: '.swiper-button-next',
      prevEl: '.swiper-button-prev',
    },
  });

   // img slider 
   var swiper = new Swiper('.img_slid', {
    // effect: "cards",
    grabCursor: true,
    autoplay: {
      speed: 1500,
      delay: 2000,
      pauseOnMouseEnter: true,
    },
    pagination: {
      el: '.hotelhub-slider-dots3',
      clickable: true,
    },
  });



  $('.venobox').venobox({
    numeratio: true,

    infinigall: true,
  });

  jQuery(document).ready(function ($) {
    'use strict';

    /* Portfolio Isotope  */
    $('.image_load').imagesLoaded(function () {
      if ($.fn.isotope) {
        var $portfolio = $('.image_load');

        $portfolio.isotope({
          itemSelector: '.grid-item',

          filter: '*',

          resizesContainer: true,

          layoutMode: 'masonry',

          transitionDuration: '0.8s',
        });
        $('.menu-filtering li').on('click', function () {
          $('.menu-filtering li').removeClass('current_menu_item');

          $(this).addClass('current_menu_item');

          var selector = $(this).attr('data-filter');

          $portfolio.isotope({
            filter: selector,
          });
        });
      }
    });
    // =======< accordion js >========
    // =======< accordion js >========
    jQuery(document).ready(function ($) {
      'use strict';

      $('.accordion > li:eq(0) a').addClass('active').next().slideDown();
      $('.accordion a').on('click', function (j) {
        let dropDown = $(this).closest('li').find('p');

        $(this).closest('.accordion').find('p').not(dropDown).slideUp();

        if ($(this).hasClass('active')) {
          $(this).removeClass('active');
        } else {
          $(this).closest('.accordion').find('a.active').removeClass('active');
          $(this).addClass('active');
        }

        dropDown.stop(false, true).slideToggle();

        j.preventDefault();
      });
    });

    jQuery(document).ready(function ($) {
      'use strict';

      $('.accordion2 > li:eq(0) a').addClass('active').next().slideDown();
      $('.accordion2 a').on('click', function (j) {
        let dropDown = $(this).closest('li').find('p');

        $(this).closest('.accordion2').find('p').not(dropDown).slideUp();

        if ($(this).hasClass('active')) {
          $(this).removeClass('active');
        } else {
          $(this).closest('.accordion2').find('a.active').removeClass('active');
          $(this).addClass('active');
        }

        dropDown.stop(false, true).slideToggle();

        j.preventDefault();
      });
    });

    //=====< barfiller script >====
    $('#bar1').barfiller({
      duration: 7000,
    });
    $('#bar2').barfiller({
      duration: 7000,
    });
    $('#bar3').barfiller({
      duration: 7000,
    });
    $('#bar4').barfiller({
      duration: 7000,
    });
    $('#bar5').barfiller({
      duration: 7000,
    });

    //======< Custom Tab >======
    $('.tab ul.tabs').addClass('active').find('> li:eq(0)').addClass('current');

    $('.tab ul.tabs li a').on('click', function (g) {
      var tab = $(this).closest('.tab'),
        index = $(this).closest('li').index();

      tab.find('ul.tabs > li').removeClass('current');
      $(this).closest('li').addClass('current');

      tab
        .find('.tab_content')
        .find('div.tabs_item')
        .not('div.tabs_item:eq(' + index + ')')
        .slideUp();
      tab
        .find('.tab_content')
        .find('div.tabs_item:eq(' + index + ')')
        .slideDown();

      g.preventDefault();
    });
  });


})(jQuery);


