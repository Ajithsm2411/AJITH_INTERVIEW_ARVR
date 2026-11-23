import sqlite3 from 'sqlite3';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const dbDirectory = path.join(__dirname, '../../data');
if (!fs.existsSync(dbDirectory)) {
  fs.mkdirSync(dbDirectory, { recursive: true });
}

const dbPath = process.env.DATABASE_PATH || path.join(dbDirectory, 'interview_platform.db');

class DatabaseManager {
  constructor() {
    this.db = null;
    this.initialized = false;
  }

  async connect() {
    if (this.initialized) return this.db;

    return new Promise((resolve, reject) => {
      this.db = new sqlite3.Database(dbPath, (err) => {
        if (err) {
          console.error('[Database] Connection failed:', err.message);
          return reject(err);
        }
        console.log(`[Database] SQLite database connected at: ${dbPath}`);
        this.runMigrations()
          .then(() => {
            this.initialized = true;
            resolve(this.db);
          })
          .catch(reject);
      });
    });
  }

  async runMigrations() {
    const schemaPath = path.join(__dirname, 'schema.sql');
    if (!fs.existsSync(schemaPath)) {
      console.warn('[Database] Schema file not found at:', schemaPath);
      return;
    }

    const schemaSql = fs.readFileSync(schemaPath, 'utf8');
    return new Promise((resolve, reject) => {
      this.db.exec(schemaSql, (err) => {
        if (err) {
          console.error('[Database] Schema execution error:', err.message);
          return reject(err);
        }
        console.log('[Database] Schema migrations executed successfully.');
        resolve();
      });
    });
  }

  run(sql, params = []) {
    return new Promise((resolve, reject) => {
      this.db.run(sql, params, function (err) {
        if (err) return reject(err);
        resolve({ lastID: this.lastID, changes: this.changes });
      });
    });
  }

  get(sql, params = []) {
    return new Promise((resolve, reject) => {
      this.db.get(sql, params, (err, row) => {
        if (err) return reject(err);
        resolve(row);
      });
    });
  }

  all(sql, params = []) {
    return new Promise((resolve, reject) => {
      this.db.all(sql, params, (err, rows) => {
        if (err) return reject(err);
        resolve(rows || []);
      });
    });
  }

  close() {
    return new Promise((resolve, reject) => {
      if (!this.db) return resolve();
      this.db.close((err) => {
        if (err) return reject(err);
        this.initialized = false;
        console.log('[Database] Database connection closed.');
        resolve();
      });
    });
  }
}

export const dbManager = new DatabaseManager();
export default dbManager;
