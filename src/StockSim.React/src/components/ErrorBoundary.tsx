import React from "react";

type State = { hasError: boolean; message?: string };

export class ErrorBoundary extends React.Component<React.PropsWithChildren, State> {
  constructor(props: React.PropsWithChildren) {
    super(props);
    this.state = { hasError: false };
  }
  static getDerivedStateFromError(err: any): State {
    return { hasError: true, message: err?.message ?? "Unknown error" };
  }
  componentDidCatch(error: any, info: any) {
    // eslint-disable-next-line no-console
    console.error("ErrorBoundary caught:", error, info);
  }
  render() {
    if (this.state.hasError) {
      return (
        <div className="card" style={{ margin: 16 }}>
          <h3>Something went wrong.</h3>
          <div style={{ color: "var(--muted)" }}>{this.state.message}</div>
          <button className="btn" onClick={() => this.setState({ hasError: false, message: undefined })} style={{ marginTop: 12 }}>
            Dismiss
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}