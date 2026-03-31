# Community Lounge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a creative community board with neon/glass theme, featuring user authentication, post CRUD, and nested comments.

**Architecture:** Node.js + Express backend with SQLite database, EJS templating, and vanilla JavaScript frontend. Session-based authentication with bcrypt password hashing.

**Tech Stack:** Node.js, Express, SQLite3, EJS, bcrypt, express-session, vanilla CSS/JS

---

## File Structure

```
community-lounge/
├── app.js                      # Main Express server
├── package.json               # Dependencies
├── .env                       # Environment variables
├── db/
│   ├── database.js           # SQLite connection & initialization
│   └── lounge.db             # SQLite database file
├── routes/
│   ├── index.js              # Home/post listing routes
│   ├── auth.js               # Login/logout/register routes
│   ├── posts.js              # Post CRUD routes
│   └── comments.js           # Comment/reply routes
├── middleware/
│   └── auth.js               # Authentication middleware
├── public/
│   ├── css/
│   │   └── style.css         # Neon/glass theme styles
│   └── js/
│       └── main.js           # Client-side interactivity
└── views/
    ├── layout.ejs            # Common layout with glass header
    ├── index.ejs             # Post list page
    ├── post-detail.ejs       # Post detail with comments
    ├── post-form.ejs         # Create/edit post form
    ├── login.ejs             # Login page with neon logo
    └── register.ejs          # Registration page
```

---

## Phase 1: Project Setup & Database

### Task 1: Initialize Node.js Project

**Files:**
- Create: `package.json`
- Create: `.env`
- Create: `app.js` (initial structure)

- [ ] **Step 1: Create package.json**

```json
{
  "name": "community-lounge",
  "version": "1.0.0",
  "description": "Creative community board with neon theme",
  "main": "app.js",
  "scripts": {
    "start": "node app.js",
    "dev": "nodemon app.js",
    "test": "jest"
  },
  "dependencies": {
    "express": "^4.18.2",
    "ejs": "^3.1.9",
    "express-ejs-layouts": "^2.5.1",
    "sqlite3": "^5.1.6",
    "bcrypt": "^5.1.1",
    "express-session": "^1.17.3",
    "dotenv": "^16.3.1"
  },
  "devDependencies": {
    "nodemon": "^3.0.1"
  }
}
```

- [ ] **Step 2: Create .gitignore file**

```
# Dependencies
node_modules/

# Environment
.env

# Database
db/*.db

# Logs
*.log
npm-debug.log*

# OS
.DS_Store
Thumbs.db
```

- [ ] **Step 3: Create .env file**

```
PORT=3000
SESSION_SECRET=community-lounge-secret-key-2024
NODE_ENV=development
```

- [ ] **Step 4: Install dependencies**

```bash
npm install
```

Expected: All dependencies installed successfully

- [ ] **Step 5: Commit**

```bash
git add package.json package-lock.json .env .gitignore
git commit -m "chore: initialize Node.js project with dependencies"
```

---

### Task 2: Setup SQLite Database

**Files:**
- Create: `db/database.js`
- Test: Check tables created

- [ ] **Step 1: Create database connection module**

```javascript
// db/database.js
const sqlite3 = require('sqlite3').verbose();
const path = require('path');

const dbPath = path.join(__dirname, 'lounge.db');
const db = new sqlite3.Database(dbPath, (err) => {
  if (err) {
    console.error('Error opening database:', err.message);
  } else {
    console.log('Connected to SQLite database');
  }
});

// Initialize tables
const initDatabase = () => {
  // Users table
  db.run(`
    CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      username TEXT UNIQUE NOT NULL,
      password TEXT NOT NULL,
      nickname TEXT UNIQUE NOT NULL,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )
  `, (err) => {
    if (err) console.error('Error creating users table:', err.message);
    else console.log('Users table ready');
  });

  // Posts table
  db.run(`
    CREATE TABLE IF NOT EXISTS posts (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL,
      title TEXT NOT NULL,
      content TEXT NOT NULL,
      view_count INTEGER DEFAULT 0,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
    )
  `, (err) => {
    if (err) console.error('Error creating posts table:', err.message);
    else console.log('Posts table ready');
  });

  // Comments table
  db.run(`
    CREATE TABLE IF NOT EXISTS comments (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      post_id INTEGER NOT NULL,
      user_id INTEGER NOT NULL,
      parent_id INTEGER,
      content TEXT NOT NULL,
      is_deleted BOOLEAN DEFAULT 0,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (post_id) REFERENCES posts(id) ON DELETE CASCADE,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
      FOREIGN KEY (parent_id) REFERENCES comments(id) ON DELETE CASCADE
    )
  `, (err) => {
    if (err) console.error('Error creating comments table:', err.message);
    else console.log('Comments table ready');
  });

  // Create triggers for updated_at
  db.run(`
    CREATE TRIGGER IF NOT EXISTS update_posts_timestamp 
    AFTER UPDATE ON posts 
    BEGIN 
      UPDATE posts SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id; 
    END;
  `, (err) => {
    if (err) console.error('Error creating posts trigger:', err.message);
    else console.log('Posts update trigger ready');
  });

  db.run(`
    CREATE TRIGGER IF NOT EXISTS update_comments_timestamp 
    AFTER UPDATE ON comments 
    BEGIN 
      UPDATE comments SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id; 
    END;
  `, (err) => {
    if (err) console.error('Error creating comments trigger:', err.message);
    else console.log('Comments update trigger ready');
  });
};

module.exports = { db, initDatabase };
```

- [ ] **Step 2: Test database initialization**

Run a test script:
```bash
node -e "const { initDatabase } = require('./db/database.js'); initDatabase(); setTimeout(() => process.exit(0), 1000);"
```

Expected output:
```
Connected to SQLite database
Users table ready
Posts table ready
Comments table ready
```

- [ ] **Step 3: Verify database file created**

```bash
ls -la db/
```

Expected: `lounge.db` file exists

- [ ] **Step 4: Commit**

```bash
git add db/
git commit -m "feat: setup SQLite database with users, posts, comments tables"
```

---

## Phase 2: Core Server & Layout

### Task 3: Setup Express Server with Middleware

**Files:**
- Modify: `app.js`
- Create: `views/layout.ejs`
- Create: `public/css/style.css` (initial)

- [ ] **Step 1: Create main Express application**

```javascript
// app.js
require('dotenv').config();
const express = require('express');
const session = require('express-session');
const expressLayouts = require('express-ejs-layouts');
const path = require('path');
const { initDatabase } = require('./db/database');

const app = express();
const PORT = process.env.PORT || 3000;

// Initialize database
initDatabase();

// Middleware
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static(path.join(__dirname, 'public')));

// EJS layouts
app.use(expressLayouts);
app.set('layout', 'layout');

// Session middleware
app.use(session({
  secret: process.env.SESSION_SECRET || 'default-secret',
  resave: false,
  saveUninitialized: false,
  cookie: { 
    secure: false,
    maxAge: 24 * 60 * 60 * 1000 // 24 hours
  }
}));

// Set view engine
app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));

// Make user available in all views
app.use((req, res, next) => {
  res.locals.user = req.session.user || null;
  next();
});

// Routes (will be added in next tasks)
// app.use('/', require('./routes/index'));
// app.use('/auth', require('./routes/auth'));
// app.use('/posts', require('./routes/posts'));
// app.use('/comments', require('./routes/comments'));

// Test route
app.get('/', (req, res) => {
  res.send('Community Lounge Server is running!');
});

// Error handling
app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).send('Something broke!');
});

app.listen(PORT, () => {
  console.log(`Community Lounge server running on http://localhost:${PORT}`);
});
```

- [ ] **Step 2: Test server starts**

```bash
timeout 5 node app.js
```

Expected output:
```
Connected to SQLite database
Users table ready
Posts table ready
Comments table ready
Community Lounge server running on http://localhost:3000
```

- [ ] **Step 3: Commit**

```bash
git add app.js
git commit -m "feat: setup Express server with middleware and session"
```

---

### Task 4: Create Base Layout with Glass Theme

**Files:**
- Create: `views/layout.ejs`
- Create: `public/css/style.css` (glass/neon theme)
- Create: `public/js/main.js` (basic)

- [ ] **Step 1: Create layout.ejs with glass header**

```html
<!-- views/layout.ejs -->
<!DOCTYPE html>
<html lang="ko">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title><%= title %> | Community Lounge</title>
  <link rel="stylesheet" href="/css/style.css">
</head>
<body>
  <!-- Glass Header -->
  <header class="glass-header">
    <div class="container">
      <a href="/" class="logo">
        <span class="logo-text">Community Lounge</span>
      </a>
      <nav class="nav-menu">
        <% if (user) { %>
          <span class="user-info">
            <span class="neon-text pink"><%= user.nickname %></span>님
          </span>
          <a href="/posts/new" class="btn btn-neon-blue">글쓰기</a>
          <a href="/auth/logout" class="btn btn-secondary">로그아웃</a>
        <% } else { %>
          <a href="/auth/login" class="btn btn-neon-pink">로그인</a>
          <a href="/auth/register" class="btn btn-secondary">회원가입</a>
        <% } %>
      </nav>
    </div>
  </header>

  <!-- Main Content -->
  <main class="main-content">
    <div class="container">
      <%- body %>
    </div>
  </main>

  <!-- Footer -->
  <footer class="glass-footer">
    <div class="container">
      <p class="footer-text">© 2026 Community Lounge</p>
    </div>
  </footer>

  <script src="/js/main.js"></script>
</body>
</html>
```

- [ ] **Step 2: Create glass/neon CSS theme**

```css
/* public/css/style.css */

/* CSS Variables */
:root {
  --bg-gradient-start: #1a1a2e;
  --bg-gradient-mid: #16213e;
  --bg-gradient-end: #0f3460;
  
  --neon-pink: #ff006e;
  --neon-blue: #00d9ff;
  --neon-purple: #9d4edd;
  --neon-orange: #ff8500;
  --neon-green: #00f5d4;
  
  --text-primary: #ffffff;
  --text-secondary: #e0e0e0;
  --text-muted: #a0a0a0;
  
  --glass-bg: rgba(255, 255, 255, 0.05);
  --glass-border: rgba(255, 255, 255, 0.1);
  --glass-shadow: rgba(0, 0, 0, 0.3);
}

/* Reset & Base */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  background: linear-gradient(135deg, var(--bg-gradient-start) 0%, var(--bg-gradient-mid) 50%, var(--bg-gradient-end) 100%);
  background-attachment: fixed;
  color: var(--text-primary);
  min-height: 100vh;
  line-height: 1.6;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

/* Glass Header */
.glass-header {
  background: var(--glass-bg);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--glass-border);
  padding: 1rem 0;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 4px 30px rgba(0, 0, 0, 0.3);
}

.glass-header .container {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.logo {
  text-decoration: none;
}

.logo-text {
  font-size: 1.5rem;
  font-weight: bold;
  background: linear-gradient(135deg, var(--neon-pink), var(--neon-purple));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  text-shadow: 0 0 30px rgba(255, 0, 110, 0.5);
}

.nav-menu {
  display: flex;
  align-items: center;
  gap: 1rem;
}

/* Buttons */
.btn {
  padding: 0.5rem 1.25rem;
  border-radius: 8px;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  border: none;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.btn-neon-pink {
  background: linear-gradient(135deg, var(--neon-pink), var(--neon-purple));
  color: white;
  box-shadow: 0 4px 15px rgba(255, 0, 110, 0.4);
}

.btn-neon-pink:hover {
  box-shadow: 0 6px 25px rgba(255, 0, 110, 0.6);
  transform: translateY(-2px);
}

.btn-neon-blue {
  background: linear-gradient(135deg, var(--neon-blue), var(--neon-purple));
  color: white;
  box-shadow: 0 4px 15px rgba(0, 217, 255, 0.4);
}

.btn-neon-blue:hover {
  box-shadow: 0 6px 25px rgba(0, 217, 255, 0.6);
  transform: translateY(-2px);
}

.btn-secondary {
  background: var(--glass-bg);
  color: var(--text-secondary);
  border: 1px solid var(--glass-border);
}

.btn-secondary:hover {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.3);
}

/* Neon Text */
.neon-text {
  text-shadow: 0 0 10px currentColor;
}

.neon-text.pink {
  color: var(--neon-pink);
}

/* Main Content */
.main-content {
  padding: 2rem 0;
  min-height: calc(100vh - 200px);
}

/* Glass Card */
.glass-card {
  background: var(--glass-bg);
  backdrop-filter: blur(10px);
  border: 1px solid var(--glass-border);
  border-radius: 16px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  box-shadow: 0 8px 32px var(--glass-shadow);
  transition: all 0.3s ease;
}

.glass-card:hover {
  border-color: rgba(0, 217, 255, 0.3);
  box-shadow: 0 8px 32px rgba(0, 217, 255, 0.15);
}

/* Glass Footer */
.glass-footer {
  background: rgba(0, 0, 0, 0.3);
  border-top: 1px solid var(--glass-border);
  padding: 1.5rem 0;
  text-align: center;
  margin-top: auto;
}

.footer-text {
  color: var(--text-muted);
  font-size: 0.9rem;
}

/* User Info */
.user-info {
  margin-right: 1rem;
}

/* Responsive */
@media (max-width: 768px) {
  .glass-header .container {
    flex-direction: column;
    gap: 1rem;
  }
  
  .nav-menu {
    flex-wrap: wrap;
    justify-content: center;
  }
}
```

- [ ] **Step 3: Create basic client JS**

```javascript
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
```

- [ ] **Step 4: Create placeholder error view (minimal version for early use)**

```html
<!-- views/error.ejs -->
<!DOCTYPE html>
<html lang="ko">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>오류 | Community Lounge</title>
  <link rel="stylesheet" href="/css/style.css">
</head>
<body>
  <div class="auth-container">
    <div class="glass-card auth-card">
      <div class="auth-header">
        <h1 class="logo-text-large">Community Lounge</h1>
        <p class="auth-subtitle"><%= message || '오류가 발생했습니다' %></p>
      </div>
      
      <div class="auth-footer">
        <a href="/" class="btn btn-neon-blue btn-full">홈으로 돌아가기</a>
      </div>
    </div>
  </div>
</body>
</html>
```

**Note:** This is a temporary standalone error page. Task 9 will replace it with a layout-based version.

- [ ] **Step 5: Test CSS loads correctly**

Start server and check browser at `http://localhost:3000` (visual check)

```bash
# Background the server
node app.js &
sleep 2

# Check if server is running
curl -s http://localhost:3000 | head -5
```

Expected: "Community Lounge Server is running!" with styling

- [ ] **Step 6: Commit**

```bash
git add views/ public/
git commit -m "feat: add glass/neon theme layout and base styles"
```

---

## Phase 3: Authentication System

### Task 5: Create Auth Middleware

**Files:**
- Create: `middleware/auth.js`

- [ ] **Step 1: Create authentication middleware**

```javascript
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
```

- [ ] **Step 2: Commit**

```bash
git add middleware/
git commit -m "feat: add authentication middleware"
```

---

### Task 6: Create Auth Routes

**Files:**
- Create: `routes/auth.js`
- Create: `views/login.ejs`
- Create: `views/register.ejs`

- [ ] **Step 1: Create auth routes**

```javascript
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
      error: '아이디와 비밀번호를 입력해주세요' 
    });
  }
  
  db.get('SELECT * FROM users WHERE username = ?', [username], async (err, user) => {
    if (err) {
      return res.render('login', { 
        title: '로그인',
        error: '오류가 발생했습니다' 
      });
    }
    
    if (!user) {
      return res.render('login', { 
        title: '로그인',
        error: '아이디 또는 비밀번호가 올바르지 않습니다' 
      });
    }
    
    const match = await bcrypt.compare(password, user.password);
    
    if (!match) {
      return res.render('login', { 
        title: '로그인',
        error: '아이디 또는 비밀번호가 올바르지 않습니다' 
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
```

- [ ] **Step 2: Create login page with neon theme**

```html
<!-- views/login.ejs -->
<% layout('layout') -%>

<div class="auth-container">
  <div class="glass-card auth-card">
    <div class="auth-header">
      <h1 class="logo-text-large">Community Lounge</h1>
      <p class="auth-subtitle">크리에이티브한 사람들의 라운지</p>
    </div>
    
    <% if (success) { %>
      <div class="alert alert-success">
        <%= success %>
      </div>
    <% } %>
    
    <% if (error) { %>
      <div class="alert alert-error">
        <%= error %>
      </div>
    <% } %>
    
    <form action="/auth/login" method="POST" class="auth-form">
      <div class="form-group">
        <label for="username">아이디</label>
        <input 
          type="text" 
          id="username" 
          name="username" 
          class="form-input" 
          placeholder="아이디를 입력하세요"
          required
        >
      </div>
      
      <div class="form-group">
        <label for="password">비밀번호</label>
        <input 
          type="password" 
          id="password" 
          name="password" 
          class="form-input" 
          placeholder="비밀번호를 입력하세요"
          required
        >
      </div>
      
      <button type="submit" class="btn btn-neon-pink btn-full">
        로그인
      </button>
    </form>
    
    <div class="auth-footer">
      <p>계정이 없으신가요? <a href="/auth/register" class="neon-link">회원가입</a></p>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Create register page**

```html
<!-- views/register.ejs -->
<% layout('layout') -%>

<div class="auth-container">
  <div class="glass-card auth-card">
    <div class="auth-header">
      <h1 class="logo-text-large">Community Lounge</h1>
      <p class="auth-subtitle">함께 할 준비가 되셨나요?</p>
    </div>
    
    <% if (error) { %>
      <div class="alert alert-error">
        <%= error %>
      </div>
    <% } %>
    
    <form action="/auth/register" method="POST" class="auth-form">
      <div class="form-group">
        <label for="username">아이디</label>
        <input 
          type="text" 
          id="username" 
          name="username" 
          class="form-input <%= usernameError ? 'input-error' : '' %>" 
          placeholder="4-20자 영문/숫자"
          required
        >
        <% if (usernameError) { %>
          <span class="error-message"><%= usernameError %></span>
        <% } %>
      </div>
      
      <div class="form-group">
        <label for="password">비밀번호</label>
        <input 
          type="password" 
          id="password" 
          name="password" 
          class="form-input" 
          placeholder="6자 이상"
          required
        >
      </div>
      
      <div class="form-group">
        <label for="passwordConfirm">비밀번호 확인</label>
        <input 
          type="password" 
          id="passwordConfirm" 
          name="passwordConfirm" 
          class="form-input" 
          placeholder="비밀번호를 다시 입력하세요"
          required
        >
      </div>
      
      <div class="form-group">
        <label for="nickname">닉네임</label>
        <input 
          type="text" 
          id="nickname" 
          name="nickname" 
          class="form-input <%= nicknameError ? 'input-error' : '' %>" 
          placeholder="2-10자"
          required
        >
        <% if (nicknameError) { %>
          <span class="error-message"><%= nicknameError %></span>
        <% } %>
      </div>
      
      <button type="submit" class="btn btn-neon-pink btn-full">
        회원가입
      </button>
    </form>
    
    <div class="auth-footer">
      <p>이미 계정이 있으신가요? <a href="/auth/login" class="neon-link">로그인</a></p>
    </div>
  </div>
</div>
```

- [ ] **Step 4: Update app.js to include auth routes**

Add to app.js after middleware section:
```javascript
// Routes
app.use('/auth', require('./routes/auth'));
// Other routes will be added in next tasks
// app.use('/', require('./routes/index'));
// app.use('/posts', require('./routes/posts'));
// app.use('/comments', require('./routes/comments'));
```

Replace the test route with:
```javascript
// Home route (placeholder until index routes are created)
app.get('/', (req, res) => {
  res.render('index', { 
    title: '홈',
    posts: [],
    pagination: { page: 1, totalPages: 1 }
  });
});
```

- [ ] **Step 5: Create placeholder index.ejs**

```html
<!-- views/index.ejs -->
<% layout('layout') -%>

<div class="page-header">
  <h1 class="page-title">게시글 목록</h1>
</div>

<div class="glass-card">
  <p class="text-center">게시글이 없습니다.</p>
</div>
```

- [ ] **Step 6: Add auth form styles to CSS**

Append to `public/css/style.css`:

```css
/* Auth Styles */
.auth-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 70vh;
  padding: 2rem;
}

.auth-card {
  width: 100%;
  max-width: 450px;
  padding: 2.5rem;
}

.auth-header {
  text-align: center;
  margin-bottom: 2rem;
}

.logo-text-large {
  font-size: 2rem;
  font-weight: bold;
  background: linear-gradient(135deg, var(--neon-pink), var(--neon-purple));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
}

.auth-subtitle {
  color: var(--neon-blue);
  font-size: 1rem;
  text-shadow: 0 0 10px rgba(0, 217, 255, 0.3);
}

.auth-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  color: var(--text-secondary);
  font-size: 0.9rem;
  font-weight: 500;
}

.form-input {
  background: rgba(0, 0, 0, 0.3);
  border: 1px solid var(--glass-border);
  border-radius: 8px;
  padding: 0.75rem 1rem;
  color: var(--text-primary);
  font-size: 1rem;
  transition: all 0.3s ease;
}

.form-input:focus {
  outline: none;
  border-color: var(--neon-blue);
  box-shadow: 0 0 10px rgba(0, 217, 255, 0.3);
}

.form-input::placeholder {
  color: var(--text-muted);
}

.form-input.input-error {
  border-color: var(--neon-pink);
}

.error-message {
  color: var(--neon-pink);
  font-size: 0.85rem;
}

.btn-full {
  width: 100%;
  margin-top: 0.5rem;
}

.auth-footer {
  text-align: center;
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--glass-border);
}

.auth-footer p {
  color: var(--text-secondary);
}

.neon-link {
  color: var(--neon-blue);
  text-decoration: none;
  position: relative;
  transition: all 0.3s ease;
}

.neon-link:hover {
  text-shadow: 0 0 10px rgba(0, 217, 255, 0.5);
}

.neon-link::after {
  content: '';
  position: absolute;
  width: 0;
  height: 2px;
  bottom: -2px;
  left: 0;
  background: var(--neon-blue);
  transition: width 0.3s ease;
}

.neon-link:hover::after {
  width: 100%;
}

/* Alerts */
.alert {
  padding: 1rem;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.alert-error {
  background: rgba(255, 0, 110, 0.1);
  border: 1px solid rgba(255, 0, 110, 0.3);
  color: var(--neon-pink);
}

.alert-success {
  background: rgba(0, 245, 212, 0.1);
  border: 1px solid rgba(0, 245, 212, 0.3);
  color: var(--neon-green);
}

/* Page Header */
.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 1.75rem;
  color: var(--text-primary);
}

.text-center {
  text-align: center;
}
```

- [ ] **Step 7: Test registration and login**

1. Start server
2. Visit http://localhost:3000/auth/register
3. Create test user
4. Visit http://localhost:3000/auth/login
5. Login with created user
6. Verify header shows user nickname

- [ ] **Step 8: Commit**

```bash
git add routes/ views/ middleware/ app.js public/css/style.css
git commit -m "feat: implement user authentication with neon theme"
```

---

## Phase 4: Post CRUD

### Task 7: Create Post Routes

**Files:**
- Create: `routes/index.js`
- Create: `routes/posts.js`
- Create: `views/index.ejs` (full version)
- Create: `views/post-detail.ejs`
- Create: `views/post-form.ejs`
- Modify: `app.js` (add routes)

- [ ] **Step 1: Create index routes for post listing**

```javascript
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
```

- [ ] **Step 2: Create post routes**

```javascript
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
```

- [ ] **Step 3: Create post list view**

```html
<!-- views/index.ejs -->
<% layout('layout') -%>

<div class="page-header flex-between">
  <h1 class="page-title">게시글 목록</h1>
  <% if (user) { %>
    <a href="/posts/new" class="btn btn-neon-blue">글쓰기</a>
  <% } %>
</div>

<% if (posts.length === 0) { %>
  <div class="glass-card">
    <p class="text-center text-muted">게시글이 없습니다.</p>
  </div>
<% } else { %>
  <div class="glass-card">
    <table class="post-table">
      <thead>
        <tr>
          <th class="col-num">번호</th>
          <th class="col-title">제목</th>
          <th class="col-author">작성자</th>
          <th class="col-date">작성일</th>
          <th class="col-comments">댓글</th>
        </tr>
      </thead>
      <tbody>
        <% posts.forEach((post, index) => { %>
          <tr class="post-row">
            <td class="col-num"><%= post.id %></td>
            <td class="col-title">
              <a href="/posts/<%= post.id %>" class="post-link">
                <%= post.title %>
              </a>
            </td>
            <td class="col-author">
              <span class="author-name"><%= post.nickname || '알 수 없음' %></span>
            </td>
            <td class="col-date">
              <%= new Date(post.created_at).toLocaleDateString('ko-KR') %>
            </td>
            <td class="col-comments">
              <% if (post.comment_count > 0) { %>
                <span class="comment-badge"><%= post.comment_count %></span>
              <% } else { %>
                <span class="comment-badge empty">0</span>
              <% } %>
            </td>
          </tr>
        <% }); %>
      </tbody>
    </table>
  </div>

  <% if (pagination.totalPages > 1) { %>
    <div class="pagination">
      <% if (pagination.hasPrev) { %>
        <a href="/?page=<%= pagination.page - 1 %>" class="btn btn-secondary">이전</a>
      <% } %>
      
      <span class="page-info">
        <%= pagination.page %> / <%= pagination.totalPages %>
      </span>
      
      <% if (pagination.hasNext) { %>
        <a href="/?page=<%= pagination.page + 1 %>" class="btn btn-secondary">다음</a>
      <% } %>
    </div>
  <% } %>
<% } %>
```

- [ ] **Step 4: Create post detail view**

```html
<!-- views/post-detail.ejs -->
<% layout('layout') -%>

<div class="post-detail">
  <!-- Post Content -->
  <div class="glass-card post-content">
    <h1 class="post-title"><%= post.title %></h1>
    
    <div class="post-meta">
      <span class="author">
        <span class="neon-text pink"><%= post.nickname || '알 수 없음' %></span>
      </span>
      <span class="separator">|</span>
      <span class="date">
        <%= new Date(post.created_at).toLocaleString('ko-KR') %>
      </span>
      <span class="separator">|</span>
      <span class="views">
        조회수 <%= post.view_count %>
      </span>
    </div>
    
    <div class="post-body">
      <%= post.content %>
    </div>
    
    <% if (user && user.id === post.user_id) { %>
      <div class="post-actions">
        <a href="/posts/<%= post.id %>/edit" class="btn btn-neon-blue">수정</a>
        <form action="/posts/<%= post.id %>/delete" method="POST" class="inline-form" onsubmit="return confirm('정말 삭제하시겠습니까?')">
          <button type="submit" class="btn btn-neon-pink">삭제</button>
        </form>
      </div>
    <% } %>
  </div>

  <!-- Comments Section -->
  <div class="comments-section">
    <h2 class="section-title">
      댓글 
      <span class="comment-count"><%= comments.length + replies.length %></span>
    </h2>
    
    <!-- Comment Form -->
    <% if (user) { %>
      <div class="glass-card comment-form-card">
        <form action="/comments/<%= post.id %>" method="POST" class="comment-form">
          <textarea 
            name="content" 
            class="form-input comment-textarea" 
            placeholder="댓글을 작성하세요"
            rows="3"
            required
          ></textarea>
          <button type="submit" class="btn btn-neon-blue">댓글 작성</button>
        </form>
      </div>
    <% } else { %>
      <div class="glass-card login-prompt">
        <p>댓글을 작성하려면 <a href="/auth/login" class="neon-link">로그인</a>이 필요합니다</p>
      </div>
    <% } %>

    <!-- Comments List -->
    <div class="comments-list">
      <% if (comments.length === 0) { %>
        <div class="glass-card no-comments">
          <p class="text-center text-muted">댓글이 없습니다</p>
        </div>
      <% } else { %>
        <% comments.forEach(comment => { %>
          <div class="comment-card parent-comment" id="comment-<%= comment.id %>">
            <div class="comment-header">
              <span class="comment-author neon-text pink"><%= comment.nickname || '알 수 없음' %></span>
              <span class="comment-date">
                <%= new Date(comment.created_at).toLocaleString('ko-KR') %>
              </span>
            </div>
            
            <div class="comment-content">
              <%= comment.is_deleted ? '삭제된 댓글입니다' : comment.content %>
            </div>
            
            <% if (!comment.is_deleted) { %>
              <div class="comment-actions">
                <% if (user && user.id === comment.user_id) { %>
                  <button class="btn btn-sm btn-secondary" onclick="editComment(<%= comment.id %>)">수정</button>
                  <form action="/comments/<%= comment.id %>/delete" method="POST" class="inline-form" onsubmit="return confirm('정말 삭제하시겠습니까?')">
                    <button type="submit" class="btn btn-sm btn-neon-pink">삭제</button>
                  </form>
                <% } %>
                <% if (user) { %>
                  <button class="btn btn-sm btn-neon-purple" onclick="showReplyForm(<%= comment.id %>)">답글</button>
                <% } %>
              </div>
            <% } %>

            <!-- Replies -->
            <% const commentReplies = replies.filter(r => r.parent_id === comment.id); %>
            <% if (commentReplies.length > 0) { %>
              <div class="replies">
                <% commentReplies.forEach(reply => { %>
                  <div class="comment-card reply-comment" id="comment-<%= reply.id %>">
                    <div class="comment-header">
                      <span class="comment-author neon-text purple"><%= reply.nickname || '알 수 없음' %></span>
                      <span class="comment-date">
                        <%= new Date(reply.created_at).toLocaleString('ko-KR') %>
                      </span>
                    </div>
                    
                    <div class="comment-content">
                      <%= reply.is_deleted ? '삭제된 댓글입니다' : reply.content %>
                    </div>
                    
                    <% if (!reply.is_deleted && user && user.id === reply.user_id) { %>
                      <div class="comment-actions">
                        <button class="btn btn-sm btn-secondary" onclick="editComment(<%= reply.id %>)">수정</button>
                        <form action="/comments/<%= reply.id %>/delete" method="POST" class="inline-form" onsubmit="return confirm('정말 삭제하시겠습니까?')">
                          <button type="submit" class="btn btn-sm btn-neon-pink">삭제</button>
                        </form>
                      </div>
                    <% } %>
                  </div>
                <% }); %>
              </div>
            <% } %>

            <!-- Reply Form (hidden by default) -->
            <% if (user) { %>
              <div id="reply-form-<%= comment.id %>" class="reply-form-container" style="display: none;">
                <form action="/comments/<%= comment.id %>/reply" method="POST" class="reply-form">
                  <textarea 
                    name="content" 
                    class="form-input reply-textarea" 
                    placeholder="답글을 작성하세요"
                    rows="2"
                    required
                  ></textarea>
                  <div class="reply-actions">
                    <button type="submit" class="btn btn-neon-purple">답글 작성</button>
                    <button type="button" class="btn btn-secondary" onclick="hideReplyForm(<%= comment.id %>)">취소</button>
                  </div>
                </form>
              </div>
            <% } %>
          </div>
        <% }); %>
      <% } %>
    </div>
  </div>
</div>

<script>
function showReplyForm(commentId) {
  document.getElementById('reply-form-' + commentId).style.display = 'block';
}

function hideReplyForm(commentId) {
  document.getElementById('reply-form-' + commentId).style.display = 'none';
}

function editComment(commentId) {
  // Simple implementation - reloads page to show edit form
  // Full implementation would use AJAX
  window.location.href = '/comments/' + commentId + '/edit';
}
</script>
```

- [ ] **Step 5: Create post form view**

```html
<!-- views/post-form.ejs -->
<% layout('layout') -%>

<div class="post-form-container">
  <div class="glass-card">
    <h1 class="page-title"><%= post ? '글 수정' : '글쓰기' %></h1>
    
    <% if (error) { %>
      <div class="alert alert-error">
        <%= error %>
      </div>
    <% } %>
    
    <form action="<%= post ? '/posts/' + post.id + '/edit' : '/posts' %>" method="POST" class="post-form">
      <div class="form-group">
        <label for="title">제목</label>
        <input 
          type="text" 
          id="title" 
          name="title" 
          class="form-input" 
          placeholder="제목을 입력하세요"
          value="<%= post ? post.title : '' %>"
          maxlength="100"
          required
        >
      </div>
      
      <div class="form-group">
        <label for="content">내용</label>
        <textarea 
          id="content" 
          name="content" 
          class="form-input content-textarea" 
          placeholder="내용을 입력하세요"
          rows="10"
          maxlength="5000"
          required
        ><%= post ? post.content : '' %></textarea>
      </div>
      
      <div class="form-actions">
        <button type="submit" class="btn btn-neon-pink">
          <%= post ? '수정 완료' : '작성 완료' %>
        </button>
        <a href="<%= post ? '/posts/' + post.id : '/' %>" class="btn btn-secondary">취소</a>
      </div>
    </form>
  </div>
</div>
```

- [ ] **Step 6: Update app.js to include all routes**

```javascript
// Replace the test route with actual routes in app.js

// Routes
app.use('/auth', require('./routes/auth'));
app.use('/', require('./routes/index'));
app.use('/posts', require('./routes/posts'));
// Comments routes will be added in next task
// app.use('/comments', require('./routes/comments'));
```

- [ ] **Step 7: Add post and comment styles to CSS**

Append to `public/css/style.css`:

```css
/* Post List Table */
.post-table {
  width: 100%;
  border-collapse: collapse;
}

.post-table thead {
  background: linear-gradient(135deg, var(--neon-pink), var(--neon-purple));
}

.post-table th {
  padding: 1rem;
  text-align: left;
  font-weight: 600;
  color: white;
}

.post-table th:first-child {
  border-radius: 8px 0 0 0;
}

.post-table th:last-child {
  border-radius: 0 8px 0 0;
}

.post-row {
  border-bottom: 1px solid var(--glass-border);
  transition: all 0.3s ease;
}

.post-row:hover {
  background: rgba(255, 255, 255, 0.05);
}

.post-row td {
  padding: 1rem;
}

.col-num {
  width: 60px;
  text-align: center;
  color: var(--text-muted);
}

.col-title {
  width: auto;
}

.col-author {
  width: 120px;
}

.col-date {
  width: 100px;
  color: var(--text-muted);
  font-size: 0.9rem;
}

.col-comments {
  width: 60px;
  text-align: center;
}

.post-link {
  color: var(--text-primary);
  text-decoration: none;
  font-weight: 500;
  transition: all 0.3s ease;
}

.post-link:hover {
  color: var(--neon-blue);
  text-shadow: 0 0 10px rgba(0, 217, 255, 0.3);
}

.author-name {
  color: var(--neon-pink);
}

.comment-badge {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  background: rgba(157, 78, 221, 0.2);
  border: 1px solid rgba(157, 78, 221, 0.4);
  border-radius: 12px;
  color: var(--neon-purple);
  font-size: 0.85rem;
}

.comment-badge.empty {
  opacity: 0.5;
}

/* Pagination */
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-top: 2rem;
}

.page-info {
  color: var(--text-secondary);
}

/* Post Detail */
.post-detail {
  max-width: 900px;
  margin: 0 auto;
}

.post-title {
  font-size: 1.75rem;
  margin-bottom: 1rem;
  color: var(--text-primary);
  text-shadow: 0 0 20px rgba(255, 0, 110, 0.3);
}

.post-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--glass-border);
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.separator {
  color: var(--text-muted);
}

.post-body {
  line-height: 1.8;
  color: var(--text-primary);
  white-space: pre-wrap;
}

.post-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid var(--glass-border);
}

.inline-form {
  display: inline;
}

/* Comments Section */
.comments-section {
  margin-top: 2rem;
}

.section-title {
  font-size: 1.25rem;
  margin-bottom: 1rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.comment-count {
  background: var(--glass-bg);
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.9rem;
  color: var(--neon-blue);
}

/* Comment Form */
.comment-form-card {
  margin-bottom: 1.5rem;
}

.comment-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.comment-textarea {
  resize: vertical;
  min-height: 80px;
}

/* Comments List */
.comments-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.comment-card {
  background: var(--glass-bg);
  border: 1px solid var(--glass-border);
  border-radius: 12px;
  padding: 1rem;
  transition: all 0.3s ease;
}

.comment-card:hover {
  border-color: rgba(0, 217, 255, 0.2);
}

.parent-comment {
  border-left: 3px solid var(--neon-pink);
}

.reply-comment {
  margin-left: 1.5rem;
  margin-top: 0.75rem;
  border-left: 3px solid var(--neon-purple);
}

.comment-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.comment-author {
  font-weight: 600;
}

.neon-text.purple {
  color: var(--neon-purple);
}

.comment-date {
  font-size: 0.85rem;
  color: var(--text-muted);
}

.comment-content {
  line-height: 1.6;
  margin-bottom: 0.75rem;
}

.comment-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-sm {
  padding: 0.35rem 0.75rem;
  font-size: 0.85rem;
}

.btn-neon-purple {
  background: linear-gradient(135deg, var(--neon-purple), var(--neon-pink));
  color: white;
  box-shadow: 0 4px 15px rgba(157, 78, 221, 0.4);
}

.btn-neon-purple:hover {
  box-shadow: 0 6px 25px rgba(157, 78, 221, 0.6);
  transform: translateY(-2px);
}

/* Reply Form */
.reply-form-container {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--glass-border);
}

.reply-form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.reply-textarea {
  resize: vertical;
  min-height: 60px;
}

.reply-actions {
  display: flex;
  gap: 0.75rem;
}

/* Post Form */
.post-form-container {
  max-width: 800px;
  margin: 0 auto;
}

.post-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.content-textarea {
  resize: vertical;
  min-height: 200px;
}

.form-actions {
  display: flex;
  gap: 1rem;
}

/* Utility */
.flex-between {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.text-muted {
  color: var(--text-muted);
}

.mb-4 {
  margin-bottom: 1.5rem;
}

.no-comments {
  padding: 2rem;
}

.login-prompt {
  text-align: center;
  padding: 1.5rem;
}
```

- [ ] **Step 8: Test post CRUD**

1. Start server
2. Login with test user
3. Create a post
4. View post list
5. View post detail
6. Edit post
7. Delete post

- [ ] **Step 9: Commit**

```bash
git add routes/ views/ public/css/style.css app.js
git commit -m "feat: implement post CRUD with neon theme"
```

---

## Phase 5: Comment System

### Task 8: Create Comment Routes

**Files:**
- Create: `routes/comments.js`
- Modify: `app.js` (add comment routes)
- Modify: `public/js/main.js` (add comment interactivity)

- [ ] **Step 1: Create comment routes**

```javascript
// routes/comments.js
const express = require('express');
const router = express.Router();
const { db } = require('../db/database');
const { requireAuth } = require('../middleware/auth');

// Create comment
router.post('/:postId', requireAuth, (req, res) => {
  const postId = req.params.postId;
  const { content } = req.body;
  const userId = req.session.user.id;
  
  if (!content || !content.trim()) {
    return res.redirect(`/posts/${postId}`);
  }
  
  db.run('INSERT INTO comments (post_id, user_id, content) VALUES (?, ?, ?)',
    [postId, userId, content.trim()],
    function(err) {
      if (err) {
        console.error('Comment creation error:', err);
      }
      res.redirect(`/posts/${postId}`);
    }
  );
});

// Create reply
router.post('/:parentId/reply', requireAuth, (req, res) => {
  const parentId = req.params.parentId;
  const { content } = req.body;
  const userId = req.session.user.id;
  
  if (!content || !content.trim()) {
    // Get post ID from parent comment
    db.get('SELECT post_id FROM comments WHERE id = ?', [parentId], (err, row) => {
      if (row) {
        res.redirect(`/posts/${row.post_id}`);
      } else {
        res.redirect('/');
      }
    });
    return;
  }
  
  // Get post ID from parent comment
  db.get('SELECT post_id FROM comments WHERE id = ?', [parentId], (err, parent) => {
    if (err || !parent) {
      return res.redirect('/');
    }
    
    db.run('INSERT INTO comments (post_id, user_id, parent_id, content) VALUES (?, ?, ?, ?)',
      [parent.post_id, userId, parentId, content.trim()],
      function(err) {
        if (err) {
          console.error('Reply creation error:', err);
        }
        res.redirect(`/posts/${parent.post_id}`);
      }
    );
  });
});

// Edit comment page
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
    
    res.render('comment-edit', {
      title: '댓글 수정',
      comment: comment,
      error: null
    });
  });
});

// Update comment
router.post('/:id/edit', requireAuth, (req, res) => {
  const commentId = req.params.id;
  const userId = req.session.user.id;
  const { content } = req.body;
  
  if (!content || !content.trim()) {
    db.get('SELECT * FROM comments WHERE id = ?', [commentId], (err, comment) => {
      res.render('comment-edit', {
        title: '댓글 수정',
        comment: comment,
        error: '내용을 입력해주세요'
      });
    });
    return;
  }
  
  db.get('SELECT post_id, user_id FROM comments WHERE id = ?', [commentId], (err, comment) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!comment) {
      return res.status(404).send('존재하지 않는 댓글입니다');
    }
    
    if (comment.user_id !== userId) {
      return res.status(403).send('작성자만 수정할 수 있습니다');
    }
    
    db.run('UPDATE comments SET content = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?',
      [content.trim(), commentId],
      function(err) {
        if (err) {
          return res.status(500).send('수정 중 오류가 발생했습니다');
        }
        res.redirect(`/posts/${comment.post_id}`);
      }
    );
  });
});

// Delete comment (soft delete)
router.post('/:id/delete', requireAuth, (req, res) => {
  const commentId = req.params.id;
  const userId = req.session.user.id;
  
  db.get('SELECT post_id, user_id FROM comments WHERE id = ?', [commentId], (err, comment) => {
    if (err) {
      return res.status(500).send('Database error');
    }
    
    if (!comment) {
      return res.status(404).send('존재하지 않는 댓글입니다');
    }
    
    if (comment.user_id !== userId) {
      return res.status(403).send('작성자만 삭제할 수 있습니다');
    }
    
    // Soft delete (mark as deleted)
    db.run('UPDATE comments SET is_deleted = 1, content = "삭제된 댓글입니다" WHERE id = ?',
      [commentId],
      function(err) {
        if (err) {
          return res.status(500).send('삭제 중 오류가 발생했습니다');
        }
        res.redirect(`/posts/${comment.post_id}`);
      }
    );
  });
});

module.exports = router;
```

- [ ] **Step 2: Create comment edit view**

```html
<!-- views/comment-edit.ejs -->
<% layout('layout') -%>

<div class="post-form-container">
  <div class="glass-card">
    <h1 class="page-title">댓글 수정</h1>
    <p class="text-muted mb-4">게시글: <%= comment.post_title %></p>
    
    <% if (error) { %>
      <div class="alert alert-error">
        <%= error %>
      </div>
    <% } %>
    
    <form action="/comments/<%= comment.id %>/edit" method="POST" class="post-form">
      <div class="form-group">
        <label for="content">내용</label>
        <textarea 
          id="content" 
          name="content" 
          class="form-input content-textarea" 
          placeholder="댓글 내용을 입력하세요"
          rows="5"
          maxlength="1000"
          required
        ><%= comment.content %></textarea>
      </div>
      
      <div class="form-actions">
        <button type="submit" class="btn btn-neon-pink">수정 완료</button>
        <a href="/posts/<%= comment.post_id %>" class="btn btn-secondary">취소</a>
      </div>
    </form>
  </div>
</div>
```

- [ ] **Step 3: Update app.js to include comment routes**

Add after other routes:
```javascript
app.use('/comments', require('./routes/comments'));
```

- [ ] **Step 4: Update main.js with comment interactivity**

Replace `public/js/main.js`:

```javascript
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
  
  // Smooth scroll for anchor links
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
      e.preventDefault();
      const target = document.querySelector(this.getAttribute('href'));
      if (target) {
        target.scrollIntoView({ behavior: 'smooth' });
      }
    });
  });
  
  // Auto-resize textareas
  const textareas = document.querySelectorAll('textarea');
  textareas.forEach(textarea => {
    textarea.addEventListener('input', function() {
      this.style.height = 'auto';
      this.style.height = this.scrollHeight + 'px';
    });
  });
});

// Reply form functions (global scope for onclick)
window.showReplyForm = function(commentId) {
  const form = document.getElementById('reply-form-' + commentId);
  if (form) {
    form.style.display = 'block';
    const textarea = form.querySelector('textarea');
    if (textarea) {
      textarea.focus();
    }
  }
};

window.hideReplyForm = function(commentId) {
  const form = document.getElementById('reply-form-' + commentId);
  if (form) {
    form.style.display = 'none';
  }
};

window.editComment = function(commentId) {
  window.location.href = '/comments/' + commentId + '/edit';
};
```

- [ ] **Step 5: Test comment system**

1. Login
2. Go to a post
3. Add a comment
4. Add a reply to comment
5. Edit comment
6. Delete comment

- [ ] **Step 6: Commit**

```bash
git add routes/comments.js views/comment-edit.ejs public/js/main.js app.js
git commit -m "feat: implement comment and reply system"
```

---

## Phase 6: Error Pages & Polish

### Task 9: Add Error Pages

**Files:**
- Create: `views/error.ejs`
- Modify: `app.js` (improve error handling)

- [ ] **Step 1: Update error page to use layout (replaces Task 4 version)****

```html
<!-- views/error.ejs -->
<% layout('layout') -%>

<div class="auth-container">
  <div class="glass-card auth-card">
    <div class="auth-header">
      <h1 class="logo-text-large">앗!</h1>
      <p class="auth-subtitle"><%= message || '오류가 발생했습니다' %></p>
    </div>
    
    <div class="auth-footer">
      <a href="/" class="btn btn-neon-blue btn-full">홈으로 돌아가기</a>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Update app.js error handling**

Update error handler in app.js:

```javascript
// Error handling
app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).render('error', {
    title: '오류',
    message: '서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요.'
  });
});

// 404 handler
app.use((req, res) => {
  res.status(404).render('error', {
    title: '페이지를 찾을 수 없습니다',
    message: '요청하신 페이지를 찾을 수 없습니다.'
  });
});
```

- [ ] **Step 3: Test error pages**

1. Visit non-existent URL
2. Verify 404 page shows

- [ ] **Step 4: Commit**

```bash
git add views/error.ejs app.js
git commit -m "feat: add error pages and improve error handling"
```

---

### Task 10: Final Polish

**Files:**
- Modify: `package.json` (add start script)
- Create: `README.md`

- [ ] **Step 1: Update package.json with better scripts**

```json
{
  "name": "community-lounge",
  "version": "1.0.0",
  "description": "Creative community board with neon glass theme",
  "main": "app.js",
  "scripts": {
    "start": "node app.js",
    "dev": "nodemon app.js",
    "init-db": "node -e \"const { initDatabase } = require('./db/database.js'); initDatabase(); setTimeout(() => process.exit(0), 1000);\""
  },
  "keywords": ["community", "board", "neon", "glassmorphism"],
  "author": "",
  "license": "MIT",
  "dependencies": {
    "express": "^4.18.2",
    "ejs": "^3.1.9",
    "sqlite3": "^5.1.6",
    "bcrypt": "^5.1.1",
    "express-session": "^1.17.3",
    "dotenv": "^16.3.1"
  },
  "devDependencies": {
    "nodemon": "^3.0.1"
  }
}
```

- [ ] **Step 2: Create README.md**

```markdown
# Community Lounge

크리에이티브한 사람들을 위한 네온 테마 커뮤니티 게시판

## Features

- ✨ 네온/글라스모피즘 디자인
- 👤 회원가입 / 로그인
- 📝 게시글 CRUD (작성자만 수정/삭제)
- 💬 댓글 및 대댓글
- 🔒 권한 관리 (비회원 읽기만 가능)

## Tech Stack

- Node.js
- Express.js
- SQLite3
- EJS
- Vanilla CSS/JS

## Installation

```bash
# Install dependencies
npm install

# Initialize database
npm run init-db

# Start server
npm start

# Or with dev mode (auto-reload)
npm run dev
```

## Environment Variables

Create `.env` file:

```
PORT=3000
SESSION_SECRET=your-secret-key
NODE_ENV=development
```

## Usage

1. Open http://localhost:3000
2. Register an account
3. Start posting!

## License

MIT
```

- [ ] **Step 3: Final test**

Run through all features:
- Register
- Login
- Create post
- Edit post
- Delete post
- Add comment
- Add reply
- Edit comment
- Delete comment
- Logout

- [ ] **Step 4: Final commit**

```bash
git add package.json README.md
git commit -m "docs: add README and final polish"
```

---

## Summary

This implementation plan creates a complete community board with:

1. **Phase 1:** Project setup and database
2. **Phase 2:** Express server and glass/neon layout
3. **Phase 3:** User authentication system
4. **Phase 4:** Post CRUD operations
5. **Phase 5:** Comment and reply system
6. **Phase 6:** Error handling and polish

Each task is bite-sized (2-5 minutes) and includes exact commands and expected outputs.
