// routes/index.js
const express = require('express');
const router = express.Router();
const { db } = require('../db/database');

// Home / Post list
router.get('/', (req, res) => {
  const page = parseInt(req.query.page) || 1;
  const limit = 10;
  const offset = (page - 1) * limit;
  
  // Get total count
  db.get('SELECT COUNT(*) as count FROM posts', [], (err, row) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    const totalPosts = row.count;
    const totalPages = Math.ceil(totalPosts / limit);
    
    // Get posts for current page
    db.all(`
      SELECT p.*, u.nickname, 
        (SELECT COUNT(*) FROM comments WHERE post_id = p.id AND is_deleted = 0) as comment_count
      FROM posts p
      LEFT JOIN users u ON p.user_id = u.id
      ORDER BY p.created_at DESC
      LIMIT ? OFFSET ?
    `, [limit, offset], (err, posts) => {
      if (err) {
        return res.status(500).send('Database error');
      }
      
      res.render('index', { 
        title: '게시글 목록',
        posts: posts,
        pagination: {
          page: page,
          totalPages: totalPages,
          hasPrev: page > 1,
          hasNext: page < totalPages
        }
      });
    });
  });
});

module.exports = router;
