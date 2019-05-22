import BaseModel from "src/app/shared/models/base-model";

export default class SiteSettingModel extends BaseModel {
  title: string;
  subtitle?: string;
  coverUrl: string;
  copyright: string;
  siteName: string;
  articleTemplate?: string;
  keywords?: string;
  private _gitment?: GitmentSetting;
  set gitment(val) {
    this._gitment = JSON.parse(val.toString());
  }
  get gitment() {
    return this._gitment;
  }

  constructor(init?: Partial<SiteSettingModel>) {
    super(init);
    Object.assign(this, init);
  }
}

export class GitmentSetting {
  githubId: string;
  repositoryName: string;
  clientId: string;
  clientSecret: string;
}
