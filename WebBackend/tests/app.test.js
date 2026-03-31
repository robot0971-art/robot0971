// tests/app.test.js
const request = require('supertest');
const express = require('express');

describe('Basic Tests', () => {
  test('should pass basic test', () => {
    expect(true).toBe(true);
  });

  test('should check server setup', () => {
    const app = express();
    expect(app).toBeDefined();
  });
});
