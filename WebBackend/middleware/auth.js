// middleware/auth.js

// Check if user is logged in
const requireAuth = (req, res, next) => {
  if (req.session && req.session.user) {
    next();
  } else {
    res.redirect('/auth/login');
  }
};

// Check if user is NOT logged in (for login/register pages)
const requireGuest = (req, res, next) => {
  if (req.session && req.session.user) {
    res.redirect('/');
  } else {
    next();
  }
};

// Check if user is the author of a post/comment
const requireAuthor = (db, table) => {
  return (req, res, next) => {
    const id = req.params.id;
    const userId = req.session.user.id;
    
    db.get(`SELECT user_id FROM ${table} WHERE id = ?`, [id], (err, row) => {
      if (err) {
        return res.status(500).send('Database error');
      }
      if (!row) {
        return res.status(404).send('Not found');
      }
      if (row.user_id !== userId) {
        return res.status(403).send('작성자만 수정/삭제할 수 있습니다');
      }
      next();
    });
  };
};

module.exports = { requireAuth, requireGuest, requireAuthor };
