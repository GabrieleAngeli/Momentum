import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { FeatureARemoteComponent } from './feature-a.component';

@NgModule({
  imports: [
    RouterModule.forChild([
      { path: '', component: FeatureARemoteComponent }
    ])
  ]
})
export class FeatureARemoteModule {}
