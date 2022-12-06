import React, { ComponentType } from 'react';
import { API } from '@storybook/api';
interface AssertionResult {
    status: string;
    fullName: string;
    title: string;
    failureMessages: string[];
}
export interface Test {
    name: string;
    result: {
        status: string;
        assertionResults: AssertionResult[];
    };
}
interface InjectedProps {
    tests?: Test[];
}
export interface HocProps {
    api: API;
    active?: boolean;
}
export interface HocState {
    kind?: string;
    storyName?: string;
    tests?: Test[];
}
declare const provideTests: (Component: ComponentType<InjectedProps>) => {
    new (props: HocProps | Readonly<HocProps>): {
        state: HocState;
        componentDidMount(): void;
        componentWillUnmount(): void;
        onAddTests: ({ kind, storyName, tests }: HocState) => void;
        mounted: boolean;
        stopListeningOnStory: () => void;
        render(): JSX.Element;
        context: any;
        setState<K extends "kind" | "storyName" | "tests">(state: HocState | ((prevState: Readonly<HocState>, props: Readonly<HocProps>) => HocState | Pick<HocState, K>) | Pick<HocState, K>, callback?: () => void): void;
        forceUpdate(callback?: () => void): void;
        readonly props: Readonly<HocProps> & Readonly<{
            children?: React.ReactNode;
        }>;
        refs: {
            [key: string]: React.ReactInstance;
        };
        shouldComponentUpdate?(nextProps: Readonly<HocProps>, nextState: Readonly<HocState>, nextContext: any): boolean;
        componentDidCatch?(error: Error, errorInfo: React.ErrorInfo): void;
        getSnapshotBeforeUpdate?(prevProps: Readonly<HocProps>, prevState: Readonly<HocState>): any;
        componentDidUpdate?(prevProps: Readonly<HocProps>, prevState: Readonly<HocState>, snapshot?: any): void;
        componentWillMount?(): void;
        UNSAFE_componentWillMount?(): void;
        componentWillReceiveProps?(nextProps: Readonly<HocProps>, nextContext: any): void;
        UNSAFE_componentWillReceiveProps?(nextProps: Readonly<HocProps>, nextContext: any): void;
        componentWillUpdate?(nextProps: Readonly<HocProps>, nextState: Readonly<HocState>, nextContext: any): void;
        UNSAFE_componentWillUpdate?(nextProps: Readonly<HocProps>, nextState: Readonly<HocState>, nextContext: any): void;
    };
    new (props: HocProps, context: any): {
        state: HocState;
        componentDidMount(): void;
        componentWillUnmount(): void;
        onAddTests: ({ kind, storyName, tests }: HocState) => void;
        mounted: boolean;
        stopListeningOnStory: () => void;
        render(): JSX.Element;
        context: any;
        setState<K extends "kind" | "storyName" | "tests">(state: HocState | ((prevState: Readonly<HocState>, props: Readonly<HocProps>) => HocState | Pick<HocState, K>) | Pick<HocState, K>, callback?: () => void): void;
        forceUpdate(callback?: () => void): void;
        readonly props: Readonly<HocProps> & Readonly<{
            children?: React.ReactNode;
        }>;
        refs: {
            [key: string]: React.ReactInstance;
        };
        shouldComponentUpdate?(nextProps: Readonly<HocProps>, nextState: Readonly<HocState>, nextContext: any): boolean;
        componentDidCatch?(error: Error, errorInfo: React.ErrorInfo): void;
        getSnapshotBeforeUpdate?(prevProps: Readonly<HocProps>, prevState: Readonly<HocState>): any;
        componentDidUpdate?(prevProps: Readonly<HocProps>, prevState: Readonly<HocState>, snapshot?: any): void;
        componentWillMount?(): void;
        UNSAFE_componentWillMount?(): void;
        componentWillReceiveProps?(nextProps: Readonly<HocProps>, nextContext: any): void;
        UNSAFE_componentWillReceiveProps?(nextProps: Readonly<HocProps>, nextContext: any): void;
        componentWillUpdate?(nextProps: Readonly<HocProps>, nextState: Readonly<HocState>, nextContext: any): void;
        UNSAFE_componentWillUpdate?(nextProps: Readonly<HocProps>, nextState: Readonly<HocState>, nextContext: any): void;
    };
    defaultProps: {
        active: boolean;
    };
    contextType?: React.Context<any>;
};
export default provideTests;
