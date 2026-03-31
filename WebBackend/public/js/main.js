// public/js/main.js
// Community Lounge - Client side interactivity

document.addEventListener('DOMContentLoaded', () => {
  console.log('Community Lounge loaded');
  
  // Add hover effects to glass cards
  const cards = document.querySelectorAll('.glass-card');
  cards.forEach(card => {
    card.addEventListener('mouseenter', () => {
      card.style.borderColor = 'rgba(0, 217, 255, 0.3)';
    });
    card.addEventListener('mouseleave', () => {
      card.style.borderColor = 'rgba(255, 255, 255, 0.1)';
    });
  });
});
