// routes/posts.js
const express = require('express');
const router = express.Router();
const { db } = require('../db/database');
const { requireAuth } = require('../middleware/auth');

// Create post page
router.get('/new', requireAuth, (req, res) => {
  res.render('post-form', { 
    title: '글쓰기',
    post: null,
    error: null
  });
});

// Create post
router.post('/', requireAuth, (req, res) => {
  const { title, content } = req.body;
  const userId = req.session.user.id;
  
  if (!title || !title.trim()) {
    return res.render('post-form', { 
      title: '글쓰기',
      post: null,
      error: '제목을 입력해주세요'
    });
  }
  
  if (!content || !content.trim()) {
    return res.render('post-form', { 
      title: '글쓰기',
      post: null,
      error: '내용을 입력해주세요'
    });
  }
  
  db.run('INSERT INTO posts (user_id, title, content) VALUES (?, ?, ?)', 
    [userId, title.trim(), content.trim()],
    function(err) {
      if (err) {
        return res.render('post-form', { 
          title: '글쓰기',
          post: null,
          error: '게시글 작성 중 오류가 발생했습니다'
        });
      }
      res.redirect('/');
    }
  );
});

// View post
router.get('/:id', (req, res) => {
  const postId = req.params.id;
  
  // Update view count
  db.run('UPDATE posts SET view_count = view_count + 1 WHERE id = ?', [postId]);
  
  // Get post
  db.get(`
    SELECT p.*, u.nickname
    FROM posts p
    LEFT JOIN users u ON p.user_id = u.id
    WHERE p.id = ?
  `, [postId], (err, post) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!post) {
      return res.status(404).render('error', {
        title: '오류',
        message: '존재하지 않는 게시글입니다'
      });
    }
    
    // Get comments with replies
    db.all(`
      SELECT c.*, u.nickname
      FROM comments c
      LEFT JOIN users u ON c.user_id = u.id
      WHERE c.post_id = ?
      ORDER BY c.created_at ASC
    `, [postId], (err, comments) => {
      if (err) {
        return res.status(500).send('Database error');
      }
      
      // Organize comments into parent and replies
      const parentComments = comments.filter(c => !c.parent_id);
      const replies = comments.filter(c => c.parent_id);
      
      res.render('post-detail', { 
        title: post.title,
        post: post,
        comments: parentComments,
        replies: replies,
        editCommentId: null,
        editCommentContent: null,
        error: null
      });
    });
  });
});

// Edit post page
router.get('/:id/edit', requireAuth, (req, res) => {
  const postId = req.params.id;
  const userId = req.session.user.id;
  
  db.get('SELECT * FROM posts WHERE id = ?', [postId], (err, post) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!post) {
      return res.status(404).render('error', {
        title: '오류',
        message: '존재하지 않는 게시글입니다'
      });
    }
    
    if (post.user_id !== userId) {
      return res.status(403).send('작성자만 수정할 수 있습니다');
    }
    
    res.render('post-form', { 
      title: '글 수정',
      post: post,
      error: null
    });
  });
});

// Update post
router.post('/:id/edit', requireAuth, (req, res) => {
  const postId = req.params.id;
  const userId = req.session.user.id;
  const { title, content } = req.body;
  
  // Check ownership
  db.get('SELECT user_id FROM posts WHERE id = ?', [postId], (err, row) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!row) {
      return res.status(404).send('존재하지 않는 게시글입니다');
    }
    
    if (row.user_id !== userId) {
      return res.status(403).send('작성자만 수정할 수 있습니다');
    }
    
    if (!title || !title.trim()) {
      return res.render('post-form', { 
        title: '글 수정',
        post: { id: postId, title, content },
        error: '제목을 입력해주세요'
      });
    }
    
    db.run('UPDATE posts SET title = ?, content = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?',
      [title.trim(), content.trim(), postId],
      function(err) {
        if (err) {
          return res.render('post-form', { 
            title: '글 수정',
            post: { id: postId, title, content },
            error: '게시글 수정 중 오류가 발생했습니다'
          });
        }
        res.redirect(`/posts/${postId}`);
      }
    );
  });
});

// Delete post
router.post('/:id/delete', requireAuth, (req, res) => {
  const postId = req.params.id;
  const userId = req.session.user.id;
  
  db.get('SELECT user_id FROM posts WHERE id = ?', [postId], (err, row) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!row) {
      return res.status(404).send('존재하지 않는 게시글입니다');
    }
    
    if (row.user_id !== userId) {
      return res.status(403).send('작성자만 삭제할 수 있습니다');
    }
    
    db.run('DELETE FROM posts WHERE id = ?', [postId], function(err) {
      if (err) {
        return res.status(500).send('삭제 중 오류가 발생했습니다');
      }
      res.redirect('/');
    });
  });
});

module.exports = router;
