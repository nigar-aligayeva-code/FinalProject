
(function($){
  function floatLabel(inputType){
    $(inputType).each(function(){
      var $this = $(this);
      // on focus add cladd active to label
      $this.focus(function(){
        $this.next().addClass("active");
      });
      //on blur check field and remove class if needed
      $this.blur(function(){
        if($this.val() === '' || $this.val() === 'blank'){
          $this.next().removeClass();
        }
      });
    });
  }
  // just add a class of "floatLabel to the input field!"
  floatLabel(".floatLabel");
})(jQuery);




document
  .getElementById('bookingForm')
  .addEventListener('submit', function (event) {
    event.preventDefault();

    const name = document.getElementById('name').value;
    const email = document.getElementById('email').value;
    const checkin = document.getElementById('checkin').value;
    const checkout = document.getElementById('checkout').value;

    const confirmationMessage = `
        <h2>Booking Confirmation</h2>
        <p>Thank you, ${name}!</p>
        <p>Your booking from ${checkin} to ${checkout} has been confirmed.</p>
        <p>We have sent a confirmation email to ${email}.</p>
    `;

    document.getElementById('confirmation').innerHTML = confirmationMessage;
  });



