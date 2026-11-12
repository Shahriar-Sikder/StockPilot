import React from 'react';
import { HtmlView } from '../template.jsx';
import template from './index.html?raw';

export default function AboutPage() {
  return <HtmlView template={template} />;
}
