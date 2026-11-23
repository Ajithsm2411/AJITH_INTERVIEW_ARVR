import { dbManager } from '../database/database.js';
import crypto from 'crypto';

export class User {
  static async create({ email, name, role = 'candidate' }) {
    const id = 'usr_' + crypto.randomBytes(8).toString('hex');
    const sql = `INSERT INTO Users (id, email, name, role) VALUES (?, ?, ?, ?)`;
    await dbManager.run(sql, [id, email, name, role]);
    return this.findById(id);
  }

  static async findById(id) {
    return dbManager.get(`SELECT * FROM Users WHERE id = ?`, [id]);
  }

  static async findByEmail(email) {
    return dbManager.get(`SELECT * FROM Users WHERE email = ?`, [email]);
  }

  static async getOrCreateDefaultUser() {
    let user = await this.findByEmail('alex.mercer@interviewroom.local');
    if (!user) {
      user = await this.create({
        email: 'alex.mercer@interviewroom.local',
        name: 'Alex Mercer',
        role: 'candidate'
      });
    }
    return user;
  }
}
