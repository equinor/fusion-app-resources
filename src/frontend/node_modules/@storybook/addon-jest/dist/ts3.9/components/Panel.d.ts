import React from 'react';
declare const _default: {
    new (props: import("../hoc/provideJestResult").HocProps | Readonly<import("../hoc/provideJestResult").HocProps>): {
        state: import("../hoc/provideJestResult").HocState;
        componentDidMount(): void;
        componentWillUnmount(): void;
        onAddTests: ({ kind, storyName, tests }: import("../hoc/provideJestResult").HocState) => void;
        mounted: boolean;
        stopListeningOnStory: () => void;
        render(): JSX.Element;
        context: any;
        setState<K extends "kind" | "storyName" | "tests">(state: import("../hoc/provideJestResult").HocState | ((prevState: Readonly<import("../hoc/provideJestResult").HocState>, props: Readonly<import("../hoc/provideJestResult").HocProps>) => import("../hoc/provideJestResult").HocState | Pick<import("../hoc/provideJestResult").HocState, K>) | Pick<import("../hoc/provideJestResult").HocState, K>, callback?: () => void): void;
        forceUpdate(callback?: () => void): void;
        readonly props: Readonly<import("../hoc/provideJestResult").HocProps> & Readonly<{
            children?: React.ReactNode;
        }>;
        refs: {
            [key: string]: React.ReactInstance;
        };
        shouldComponentUpdate?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextState: Readonly<import("../hoc/provideJestResult").HocState>, nextContext: any): boolean;
        componentDidCatch?(error: Error, errorInfo: React.ErrorInfo): void;
        getSnapshotBeforeUpdate?(prevProps: Readonly<import("../hoc/provideJestResult").HocProps>, prevState: Readonly<import("../hoc/provideJestResult").HocState>): any;
        componentDidUpdate?(prevProps: Readonly<import("../hoc/provideJestResult").HocProps>, prevState: Readonly<import("../hoc/provideJestResult").HocState>, snapshot?: any): void;
        componentWillMount?(): void;
        UNSAFE_componentWillMount?(): void;
        componentWillReceiveProps?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextContext: any): void;
        UNSAFE_componentWillReceiveProps?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextContext: any): void;
        componentWillUpdate?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextState: Readonly<import("../hoc/provideJestResult").HocState>, nextContext: any): void;
        UNSAFE_componentWillUpdate?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextState: Readonly<import("../hoc/provideJestResult").HocState>, nextContext: any): void;
    };
    new (props: import("../hoc/provideJestResult").HocProps, context: any): {
        state: import("../hoc/provideJestResult").HocState;
        componentDidMount(): void;
        componentWillUnmount(): void;
        onAddTests: ({ kind, storyName, tests }: import("../hoc/provideJestResult").HocState) => void;
        mounted: boolean;
        stopListeningOnStory: () => void;
        render(): JSX.Element;
        context: any;
        setState<K extends "kind" | "storyName" | "tests">(state: import("../hoc/provideJestResult").HocState | ((prevState: Readonly<import("../hoc/provideJestResult").HocState>, props: Readonly<import("../hoc/provideJestResult").HocProps>) => import("../hoc/provideJestResult").HocState | Pick<import("../hoc/provideJestResult").HocState, K>) | Pick<import("../hoc/provideJestResult").HocState, K>, callback?: () => void): void;
        forceUpdate(callback?: () => void): void;
        readonly props: Readonly<import("../hoc/provideJestResult").HocProps> & Readonly<{
            children?: React.ReactNode;
        }>;
        refs: {
            [key: string]: React.ReactInstance;
        };
        shouldComponentUpdate?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextState: Readonly<import("../hoc/provideJestResult").HocState>, nextContext: any): boolean;
        componentDidCatch?(error: Error, errorInfo: React.ErrorInfo): void;
        getSnapshotBeforeUpdate?(prevProps: Readonly<import("../hoc/provideJestResult").HocProps>, prevState: Readonly<import("../hoc/provideJestResult").HocState>): any;
        componentDidUpdate?(prevProps: Readonly<import("../hoc/provideJestResult").HocProps>, prevState: Readonly<import("../hoc/provideJestResult").HocState>, snapshot?: any): void;
        componentWillMount?(): void;
        UNSAFE_componentWillMount?(): void;
        componentWillReceiveProps?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextContext: any): void;
        UNSAFE_componentWillReceiveProps?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextContext: any): void;
        componentWillUpdate?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextState: Readonly<import("../hoc/provideJestResult").HocState>, nextContext: any): void;
        UNSAFE_componentWillUpdate?(nextProps: Readonly<import("../hoc/provideJestResult").HocProps>, nextState: Readonly<import("../hoc/provideJestResult").HocState>, nextContext: any): void;
    };
    defaultProps: {
        active: boolean;
    };
    contextType?: React.Context<any>;
};
export default _default;
