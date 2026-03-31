// routes/auth.js
const express = require('express');
const bcrypt = require('bcrypt');
const router = express.Router();
const { db } = require('../db/database');
const { requireGuest } = require('../middleware/auth');

// Login page
router.get('/login', requireGuest, (req, res) => {
  const successMessage = req.query.registered === 'true' ? '회원가입이 완료되었습니다' : null;
  res.render('login', { 
    title: '로그인',
    error: null,
    success: successMessage
  });
});

// Login process
router.post('/login', requireGuest, (req, res) => {
  const { username, password } = req.body;
  
  if (!username || !password) {
    return res.render('login', { 
      title: '로그인',
      error: '아이디와 비밀번호를 입력해주세요',
      success: null
    });
  }
  
  db.get('SELECT * FROM users WHERE username = ?', [username], async (err, user) => {
    if (err) {
      return res.render('login', { 
        title: '로그인',
        error: '오류가 발생했습니다',
        success: null
      });
    }
    
    if (!user) {
      return res.render('login', { 
        title: '로그인',
        error: '아이디 또는 비밀번호가 올바르지 않습니다',
        success: null
      });
    }
    
    const match = await bcrypt.compare(password, user.password);
    
    if (!match) {
      return res.render('login', { 
        title: '로그인',
        error: '아이디 또는 비밀번호가 올바르지 않습니다',
        success: null
      });
    }
    
    req.session.user = {
      id: user.id,
      username: user.username,
      nickname: user.nickname
    };
    
    res.redirect('/');
  });
});

// Register page
router.get('/register', requireGuest, (req, res) => {
  res.render('register', { 
    title: '회원가입',
    error: null,
    usernameError: null,
    nicknameError: null
  });
});

// Register process
router.post('/register', requireGuest, async (req, res) => {
  const { username, password, passwordConfirm, nickname } = req.body;
  
  // Validation
  if (!username || !password || !nickname) {
    return res.render('register', { 
      title: '회원가입',
      error: '필수 정보를 입력해주세요',
      usernameError: null,
      nicknameError: null
    });
  }
  
  if (password !== passwordConfirm) {
    return res.render('register', { 
      title: '회원가입',
      error: '비밀번호가 일치하지 않습니다',
      usernameError: null,
      nicknameError: null
    });
  }
  
  if (username.length < 4 || username.length > 20) {
    return res.render('register', { 
      title: '회원가입',
      error: null,
      usernameError: '아이디는 4-20자로 입력해주세요',
      nicknameError: null
    });
  }
  
  if (nickname.length < 2 || nickname.length > 10) {
    return res.render('register', { 
      title: '회원가입',
      error: null,
      usernameError: null,
      nicknameError: '닉네임은 2-10자로 입력해주세요'
    });
  }
  
  try {
    // Check duplicate username
    const existingUser = await new Promise((resolve, reject) => {
      db.get('SELECT id FROM users WHERE username = ?', [username], (err, row) => {
        if (err) reject(err);
        else resolve(row);
      });
    });
    
    if (existingUser) {
      return res.render('register', { 
        title: '회원가입',
        error: null,
        usernameError: '이미 사용 중인 아이디입니다',
        nicknameError: null
      });
    }
    
    // Check duplicate nickname
    const existingNickname = await new Promise((resolve, reject) => {
      db.get('SELECT id FROM users WHERE nickname = ?', [nickname], (err, row) => {
        if (err) reject(err);
        else resolve(row);
      });
    });
    
    if (existingNickname) {
      return res.render('register', { 
        title: '회원가입',
        error: null,
        usernameError: null,
        nicknameError: '이미 사용 중인 닉네임입니다'
      });
    }
    
    // Hash password and create user
    const hashedPassword = await bcrypt.hash(password, 10);
    
    db.run('INSERT INTO users (username, password, nickname) VALUES (?, ?, ?)', 
      [username, hashedPassword, nickname], 
      (err) => {
        if (err) {
          return res.render('register', { 
            title: '회원가입',
            error: '회원가입 중 오류가 발생했습니다',
            usernameError: null,
            nicknameError: null
          });
        }
        res.redirect('/auth/login?registered=true');
      }
    );
  } catch (error) {
    res.render('register', { 
      title: '회원가입',
      error: '오류가 발생했습니다',
      usernameError: null,
      nicknameError: null
    });
  }
});

// Logout
router.get('/logout', (req, res) => {
  req.session.destroy();
  res.redirect('/');
});

// Check username availability (AJAX)
router.get('/check-username', (req, res) => {
  const { username } = req.query;
  
  if (!username) {
    return res.json({ available: false, message: '아이디를 입력해주세요' });
  }
  
  db.get('SELECT id FROM users WHERE username = ?', [username], (err, row) => {
    if (err) {
      return res.json({ available: false, message: '오류가 발생했습니다' });
    }
    
    if (row) {
      return res.json({ available: false, message: '이미 사용 중인 아이디입니다' });
    }
    
    res.json({ available: true, message: '사용 가능한 아이디입니다' });
  });
});

// Check nickname availability (AJAX)
router.get('/check-nickname', (req, res) => {
  const { nickname } = req.query;
  
  if (!nickname) {
    return res.json({ available: false, message: '닉네임을 입력해주세요' });
  }
  
  db.get('SELECT id FROM users WHERE nickname = ?', [nickname], (err, row) => {
    if (err) {
      return res.json({ available: false, message: '오류가 발생했습니다' });
    }
    
    if (row) {
      return res.json({ available: false, message: '이미 사용 중인 닉네임입니다' });
    }
    
    res.json({ available: true, message: '사용 가능한 닉네임입니다' });
  });
});

module.exports = router;
