// routes/comments.js
const express = require('express');
const router = express.Router();
const { db } = require('../db/database');
const { requireAuth } = require('../middleware/auth');

// Create comment
router.post('/:postId', requireAuth, (req, res) => {
  const postId = req.params.postId;
  const userId = req.session.user.id;
  const { content } = req.body;
  
  if (!content || !content.trim()) {
    return res.redirect(`/posts/${postId}`);
  }
  
  db.run('INSERT INTO comments (post_id, user_id, content) VALUES (?, ?, ?)',
    [postId, userId, content.trim()],
    function(err) {
      if (err) {
        console.error('Error creating comment:', err);
      }
      res.redirect(`/posts/${postId}`);
    }
  );
});

// Create reply
router.post('/:parentId/reply', requireAuth, (req, res) => {
  const parentId = req.params.parentId;
  const userId = req.session.user.id;
  const { content } = req.body;
  
  if (!content || !content.trim()) {
    return res.redirect('back');
  }
  
  // Get post_id from parent comment
  db.get('SELECT post_id FROM comments WHERE id = ?', [parentId], (err, row) => {
    if (err || !row) {
      return res.redirect('back');
    }
    
    const postId = row.post_id;
    
    db.run('INSERT INTO comments (post_id, user_id, parent_id, content) VALUES (?, ?, ?, ?)',
      [postId, userId, parentId, content.trim()],
      function(err) {
        if (err) {
          console.error('Error creating reply:', err);
        }
        res.redirect(`/posts/${postId}`);
      }
    );
  });
});

// Edit comment
router.get('/:id/edit', requireAuth, (req, res) => {
  const commentId = req.params.id;
  const userId = req.session.user.id;
  
  db.get(`
    SELECT c.*, p.title as post_title
    FROM comments c
    JOIN posts p ON c.post_id = p.id
    WHERE c.id = ?
  `, [commentId], (err, comment) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!comment) {
      return res.status(404).send('존재하지 않는 댓글입니다');
    }
    
    if (comment.user_id !== userId) {
      return res.status(403).send('작성자만 수정할 수 있습니다');
    }
    
    // Get post and comments for rendering
    db.get('SELECT * FROM posts WHERE id = ?', [comment.post_id], (err, post) => {
      if (err || !post) {
        return res.status(500).send('Database error');
      }
      
      db.all(`
        SELECT c.*, u.nickname
        FROM comments c
        LEFT JOIN users u ON c.user_id = u.id
        WHERE c.post_id = ?
        ORDER BY c.created_at ASC
      `, [comment.post_id], (err, comments) => {
        if (err) {
          return res.status(500).send('Database error');
        }
        
        const parentComments = comments.filter(c => !c.parent_id);
        const replies = comments.filter(c => c.parent_id);
        
        res.render('post-detail', { 
          title: post.title,
          post: post,
          comments: parentComments,
          replies: replies,
          editCommentId: parseInt(commentId),
          editCommentContent: comment.content,
          error: null
        });
      });
    });
  });
});

// Update comment
router.post('/:id/edit', requireAuth, (req, res) => {
  const commentId = req.params.id;
  const userId = req.session.user.id;
  const { content } = req.body;
  
  if (!content || !content.trim()) {
    return res.redirect('back');
  }
  
  db.get('SELECT user_id, post_id FROM comments WHERE id = ?', [commentId], (err, row) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!row) {
      return res.status(404).send('존재하지 않는 댓글입니다');
    }
    
    if (row.user_id !== userId) {
      return res.status(403).send('작성자만 수정할 수 있습니다');
    }
    
    db.run('UPDATE comments SET content = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?',
      [content.trim(), commentId],
      function(err) {
        if (err) {
          return res.status(500).send('수정 중 오류가 발생했습니다');
        }
        res.redirect(`/posts/${row.post_id}`);
      }
    );
  });
});

// Delete comment (soft delete)
router.post('/:id/delete', requireAuth, (req, res) => {
  const commentId = req.params.id;
  const userId = req.session.user.id;
  
  db.get('SELECT user_id, post_id FROM comments WHERE id = ?', [commentId], (err, row) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!row) {
      return res.status(404).send('존재하지 않는 댓글입니다');
    }
    
    if (row.user_id !== userId) {
      return res.status(403).send('작성자만 삭제할 수 있습니다');
    }
    
    db.run('UPDATE comments SET is_deleted = 1, content = "삭제된 댓글입니다" WHERE id = ?',
      [commentId],
      function(err) {
        if (err) {
          return res.status(500).send('삭제 중 오류가 발생했습니다');
        }
        res.redirect(`/posts/${row.post_id}`);
      }
    );
  });
});

module.exports = router;
